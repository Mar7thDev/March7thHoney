-- Client 4.6.51 / GridFight season 2. Display only: no team or combat state changes.
-- Generated from GridFightRoleConfig_Index_SeasonAndTrait.json, season 2 (29 traits / 64 roles).
-- Load: /windy load gridfight_trait_roles.lua ; then close and reopen the trait card.
local panel = require("Ui.GridFight.Common.Trait.GridFightTraitCardPanel")
local state = rawget(_G, "GridFightTraitRolesPatch")
if state == nil then
    state = { original = panel._get_trait_members, dummies = {} }
    _G.GridFightTraitRolesPatch = state
end

local trait_roles = {
    [100102] = {121002, 121402, 121802, 122002, 122102, 122202, 121502},
    [100202] = {140202, 140402, 140502, 140602, 140902, 141002, 141302},
    [100302] = {131302, 150102, 150302, 150402, 150502, 150602, 150702},
    [100402] = {110502, 110702, 120302, 122502, 130402, 131502, 140102},
    [100502] = {100402, 122402, 131302, 151002, 800902, 121302, 604202},
    [100602] = {111202, 150202, 150302, 151202, 151302, 131402},
    [100702] = {120402, 120502, 121102, 121202, 121302},
    [100802] = {100502, 131002, 150602, 150702, 800902},
    [100902] = {120802, 121702, 130602, 140302, 141202},
    [101002] = {130602, 130702, 130802, 132102, 141302},
    [101102] = {110302, 130902, 131702, 150102},
    [101202] = {140402, 140702, 140802, 141502},
    [200102] = {111202, 122002, 122402, 130402, 130902, 140302, 150402},
    [200202] = {100502, 110302, 110502, 121002, 121802, 130702, 141002},
    [200302] = {100402, 122202, 130802, 140102, 140602, 150702, 131402},
    [200402] = {121102, 121202, 131302, 140202, 140502, 141202, 121502},
    [200502] = {121402, 122502, 131002, 131502, 131702, 132102},
    [200602] = {120402, 140702, 140902, 141502, 151202, 141302},
    [200702] = {130602, 150102, 150202, 150602, 151302, 151102, 151112, 151122, 151132},
    [200802] = {110702, 120502, 120802, 122102, 140402},
    [200902] = {121702, 150502, 151002, 151202, 800902},
    [201002] = {110502, 120302, 121102, 121702, 122202, 140902},
    [201102] = {120802, 130402, 150302},
    [300102] = {141402, 121302},
    [300202] = {140802},
    [300302] = {150402},
    [300402] = {131402},
    [300502] = {110702},
    [300602] = {151102, 151112, 151122, 151132},
}

panel._get_trait_members = function(self, ...)
    local members = state.original(self, ...)
    local ids = self._trait and trait_roles[self._trait.ID]
    if ids == nil then return members end

    local seen = {}
    for index = 0, members.Count - 1 do
        local member = members[index]
        if member.MemberType == CS.RPG.Client.GridFightGridMemberType.Role then
            seen[member.ID] = true
        end
    end
    for _, id in ipairs(ids) do
        if not seen[id] then
            local role = state.dummies[id]
            if role == nil then
                local ok, value = pcall(CS.RPG.Client.GridFightRole.CreateDummy, id)
                if ok then
                    role = value
                    state.dummies[id] = role
                else
                    print("[GridFight] Trait role unavailable: " .. tostring(id) .. " " .. tostring(value))
                end
            end
            if role ~= nil then
                members:Add(role)
                seen[id] = true
            end
        end
    end
    return members
end

print("[GridFight] Season-2 trait role display patch loaded. Reopen the trait card.")
