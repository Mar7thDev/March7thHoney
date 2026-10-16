-- 4.6.51 client: supply equipment instructions only when Aha's native description is empty.
-- Load from the game's console with: /windy load gridfight_aha_text.lua
local panel = require("Ui.GridFight.Preparation.Trait.Pop.GridFightElationTraitEffectSkillPanel")
local state = rawget(_G, "GridFightAhaTextPatch")
if state == nil then
    state = { original = panel.setup_view }
    _G.GridFightAhaTextPatch = state
end

panel.setup_view = function(self, skill, ...)
    local result = state.original(self, skill, ...)
    if skill ~= nil and (skill.SkillDesc == nil or skill.SkillDesc == "") then
        self._binder.txt_name:SetCustomizedText("阿哈的装备说明")
        self._binder.txt_desc:SetCustomizedText("阿哈携带本局随机的3件简易装备。随【欢愉】羁绊层数解锁装备槽；将简易装备拖到阿哈处，可合成为对应进阶装备。")
    end
    return result
end

print("[GridFight] Aha empty-description fallback loaded. Reopen the Aha panel.")
