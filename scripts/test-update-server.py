#!/usr/bin/env python3
"""Isolated deployment regression. Uses only temporary Git repositories and mocked services/.NET."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parent.parent
MOCK = r'''#!/usr/bin/env python3
import json, os, pathlib, subprocess, sys
name=pathlib.Path(sys.argv[0]).name; args=sys.argv[1:]
with open(os.environ['TRACE'], 'a') as f: f.write(json.dumps([name,args,os.getcwd()])+'\n')
if name == 'sudo':
    sys.exit(subprocess.run(args,env=os.environ).returncode)
if name == 'systemctl':
    state=pathlib.Path(os.environ['ACTIVE'])
    if args[0]=='cat': sys.exit(0)
    if args[0]=='is-active': sys.exit(0 if state.read_text()=='1' else 3)
    if args[0]=='stop': state.write_text('0'); sys.exit(0)
    if args[0]=='start':
        root=pathlib.Path(os.environ['CHECKOUT']); ptr=root/'.git/march7thhoney-deploy/current'
        version=(ptr/'March7thHoney.dll').read_text() if ptr.is_symlink() else 'source'
        if os.environ.get('FAIL_START')==version: sys.exit(1)
        state.write_text('1')
        sys.exit(subprocess.run(['bash',str(root/'scripts/run-server.sh')],env=os.environ).returncode)
if name == 'dotnet':
    if args[0]=='publish':
        source=pathlib.Path(args[1]).parent.parent
        version=(source/'version.txt').read_text().strip()
        if os.environ.get('FAIL_PUBLISH')==version: sys.exit(42)
        out=pathlib.Path(args[args.index('-o')+1]); out.mkdir(parents=True,exist_ok=True)
        for n in ['March7thHoney.dll','March7thHoney.deps.json','March7thHoney.runtimeconfig.json']:
            (out/n).write_text(version)
        sys.exit(0)
    if args[0].endswith('March7thHoney.dll'): sys.exit(0)
    # A rollback must never rebuild the failing source tree.
    sys.exit(90)
sys.exit(91)
'''


def run(args, cwd, env=None, check=True):
    return subprocess.run(args, cwd=cwd, env=env, text=True, capture_output=True, check=check)


def scenario(name, options=(), fail_publish=None, fail_start=None, dirty=False, legacy_runner=False, existing_release=False):
    with tempfile.TemporaryDirectory(prefix='honey-deploy-test-') as temp:
        base=Path(temp); remote=base/'remote.git'; source=base/'checkout with spaces'; author=base/'author'
        run(['git','init','--bare',str(remote)],base)
        run(['git','init','-b','main',str(source)],base)
        run(['git','config','user.name','Regression'],source)
        run(['git','config','user.email','regression@localhost'],source)
        (source/'scripts').mkdir(); (source/'Program').mkdir()
        for file in ['update-server.sh','run-server.sh']:
            shutil.copy2(ROOT/'scripts'/file, source/'scripts'/file)
        if legacy_runner:
            (source/'scripts/run-server.sh').write_text('#!/usr/bin/env bash\nexec dotnet run --project Program/Program.csproj\n')
        (source/'Program/Program.csproj').write_text('<Project/>')
        (source/'version.txt').write_text('old')
        (source/'.gitignore').write_text('Config/Database/\nConfig/Logs/\n')
        run(['git','add','.'],source); run(['git','commit','-m','baseline'],source)
        old=run(['git','rev-parse','HEAD'],source).stdout.strip()
        run(['git','remote','add','origin',str(remote)],source)
        run(['git','push','-u','origin','main'],source)
        run(['git','clone','-b','main',str(remote),str(author)],base)
        run(['git','config','user.name','Regression'],author)
        run(['git','config','user.email','regression@localhost'],author)
        (author/'version.txt').write_text('new')
        run(['git','commit','-am','candidate'],author);run(['git','push'],author)
        new=run(['git','rev-parse','HEAD'],author).stdout.strip()
        mock=base/'mocks';mock.mkdir()
        for file in ['dotnet','sudo','systemctl']:
            p=mock/file;p.write_text(MOCK);p.chmod(0o755)
        if existing_release:
            state=source/'.git/march7thhoney-deploy'; old_release=state/'releases/known-good'
            old_release.mkdir(parents=True)
            for n in ['March7thHoney.dll','March7thHoney.deps.json','March7thHoney.runtimeconfig.json']:
                (old_release/n).write_text('old')
            (state/'current').symlink_to(old_release)
        active=base/'active';active.write_text('1');trace=base/'trace';trace.write_text('')
        data=source/'Config/Database/data';data.parent.mkdir(parents=True);data.write_text('keep player data')
        env=os.environ.copy();env.update(PATH=str(mock)+os.pathsep+env['PATH'],TRACE=str(trace),ACTIVE=str(active),CHECKOUT=str(source))
        if fail_publish:env['FAIL_PUBLISH']=fail_publish
        if fail_start:env['FAIL_START']=fail_start
        if dirty:(source/'version.txt').write_text('local edit')
        result=run(['bash','scripts/update-server.sh',*options],source,env,False)
        events=[json.loads(line) for line in trace.read_text().splitlines()]
        actions=[e for e in events if e[0]=='systemctl' and e[1][0] in ['stop','start']]
        head=run(['git','rev-parse','HEAD'],source).stdout.strip()
        pointer=source/'.git/march7thhoney-deploy/current'
        current=(pointer/'March7thHoney.dll').read_text() if pointer.is_symlink() else None
        assert data.read_text()=='keep player data'
        if fail_publish=='new' or (fail_publish=='old' and not existing_release) or dirty or legacy_runner or options==('--no-build',):
            assert result.returncode!=0,(name,result.stdout,result.stderr)
            assert head==old and not actions and active.read_text()=='1',(name,events)
        elif fail_start:
            assert result.returncode!=0
            assert head==old and current=='old' and active.read_text()=='1',(name,events,result.stderr)
            assert any(e[0]=='dotnet' and e[1][0].endswith('March7thHoney.dll') for e in events)
            assert not any(e[0]=='dotnet' and e[1][0] in ['build','run'] for e in events)
        elif '--no-build' in options:
            assert result.returncode==0 and head==new and current is None and not actions
        else:
            assert result.returncode==0,(name,result.stdout,result.stderr)
            assert head==new and current=='new'
            publishes=[i for i,e in enumerate(events) if e[0]=='dotnet' and e[1][0]=='publish']
            stops=[i for i,e in enumerate(events) if e[0]=='systemctl' and e[1][0]=='stop']
            assert not stops or max(publishes)<min(stops)
            if existing_release: assert len(publishes)==1
        print('PASS',name)


scenario('legacy runner refuses migration before service or checkout changes',legacy_runner=True)
scenario('candidate publish failure leaves running service and checkout untouched',fail_publish='new')
scenario('rollback snapshot failure leaves running service and checkout untouched',fail_publish='old')
scenario('successful deployment builds before downtime and runs published DLL')
scenario('startup failure restores previous source and runs old published DLL',fail_start='new')
scenario('existing immutable rollback is reused without rebuilding it',existing_release=True,fail_publish='old')
scenario('startup failure restores an existing immutable rollback',existing_release=True,fail_start='new')
scenario('dirty source is never overwritten',dirty=True)
scenario('unverified build cannot restart service',options=('--no-build',))
scenario('explicit source-only update',options=('--no-build','--no-restart'))
scenario('no-restart deploy changes selected release without stopping service',options=('--no-restart',))
print('Deployment regression failures: 0')
