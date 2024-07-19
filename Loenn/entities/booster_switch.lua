-- Modified from Loenn's src/entities/dash_switch.lua

local boosterSwitch = {}
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local textures = {
    default = "objects/temple/dashButton00",
    mirror = "objects/temple/dashButtonMirror00",
}
local textureOptions = {}

for texture, _ in pairs(textures) do
    textureOptions[utils.titleCase(texture)] = texture
end

-- Down Left Up Right
local dashSwitchDirectionLookup = {
    "down",
    "left",
    "up",
    "right",

    down = 1,
    left = 2,
    up = 3,
    right = 4,
    -- {"PortalRenderHelper/BoosterSwitch", "ceiling", false},
    -- {"PortalRenderHelper/BoosterSwitch", "leftSide", true},
    -- {"PortalRenderHelper/BoosterSwitch", "ceiling", true},
    -- {"PortalRenderHelper/BoosterSwitch", "leftSide", false},
}

local sideOptions = {}
for _, side in ipairs(dashSwitchDirectionLookup) do
    sideOptions[utils.titleCase(side)] = side
end

local function rotateCommon(entity, sideIndex, direction)
    local targetIndex = utils.mod1(sideIndex + direction, 4)

    if sideIndex ~= targetIndex then
        -- local newName, attribute, value = unpack(dashSwitchDirectionLookup[targetIndex])

        -- entity._name = newName

        -- entity.ceiling = nil
        -- entity.leftSide = nil

        -- entity[attribute] = value
        entity.side = dashSwitchDirectionLookup[targetIndex]
    end

    return sideIndex ~= targetIndex
end

-- local dashSwitchHorizontal = {}

boosterSwitch.name = "PortalRenderHelper/BoosterSwitch"
boosterSwitch.depth = 0
boosterSwitch.justification = {0.5, 0.5}
boosterSwitch.fieldInformation = {
    sprite = {
        options = textureOptions
    },
    side = {
        options = sideOptions,
        editable = false,
    },
    color = {
        fieldType = 'color',
    },
}
boosterSwitch.placements = {}

-- Down Left Up Right
local spriteSideInfo = {
    down = {8, 8, math.pi/2},
    left = {0, 8, math.pi},
    up = {8, 0, -math.pi/2},
    right = {8, 8, 0.0},
}

function boosterSwitch.sprite(room, entity)
    local texture = entity.sprite == "default" and textures["default"] or textures["mirror"]
    local sprite = drawableSprite.fromTexture(texture, entity)

    local posX, posY, rotation = unpack(spriteSideInfo[entity.side or 'down'] or spriteSideInfo[down])
    sprite:addPosition(posX, posY)
    sprite.rotation = rotation
    sprite:setColor(entity.color or '#ffbbbb')

    -- if leftSide then
    --     sprite:addPosition(0, 8)
    --     sprite.rotation = math.pi

    -- else
    --     sprite:addPosition(8, 8)
    --     sprite.rotation = 0
    -- end

    return sprite
end

-- function boosterSwitch.flip(room, entity, horizontal, vertical)
--     if horizontal then
--         entity.leftSide = not entity.leftSide
--     end

--     return horizontal
-- end

function boosterSwitch.rotate(room, entity, direction)
    local sideIndex = dashSwitchDirectionLookup[entity.side or 'down'] or 1

    return rotateCommon(entity, sideIndex, direction)
end

-- local dashSwitchVertical = {}

-- dashSwitchVertical.name = "dashSwitchV"
-- dashSwitchVertical.depth = 0
-- dashSwitchVertical.justification = {0.5, 0.5}
-- dashSwitchVertical.fieldInformation = {
--     sprite = {
--         options = textureOptions
--     }
-- }
-- dashSwitchVertical.placements = {}

-- function dashSwitchVertical.sprite(room, entity)
--     local ceiling = entity.ceiling
--     local texture = entity.sprite == "default" and textures["default"] or textures["mirror"]
--     local sprite = drawableSprite.fromTexture(texture, entity)

--     if ceiling then
--         sprite:addPosition(8, 0)
--         sprite.rotation = -math.pi / 2

--     else
--         sprite:addPosition(8, 8)
--         sprite.rotation = math.pi / 2
--     end

--     return sprite
-- end

-- function dashSwitchVertical.flip(room, entity, horizontal, vertical)
--     if vertical then
--         entity.ceiling = not entity.ceiling
--     end

--     return vertical
-- end

-- function dashSwitchVertical.rotate(room, entity, direction)
--     local sideIndex = entity.ceiling and 3 or 1

--     return rotateCommon(entity, sideIndex, direction)
-- end

-- local placementsInfo = {
--     {boosterSwitch.placements, "up", "ceiling", false},
--     {boosterSwitch.placements, "down", "ceiling", true},
--     {boosterSwitch.placements, "left", "leftSide", false},
--     {boosterSwitch.placements, "right", "leftSide", true}
-- }

for name, texture in pairs(textures) do
    -- for _, info in ipairs(placementsInfo) do
    --     local placementsTable, direction, key, value = unpack(info)
    --     local placement = {
    --         name = string.format("%s_%s", direction, name),
    --         data = {
    --             persistent = false,
    --             sprite = name,
    --             allGates = false
    --         }
    --     }

    --     placement.data[key] = value

    --     table.insert(placementsTable, placement)
    -- end
    table.insert(boosterSwitch.placements, {
        name = name,
        data = {
            persistent = false,
            sprite = name,
            allGates = false,
            side = 'up',
            color = '#ffbbbb'
        }
    })
end

return boosterSwitch
