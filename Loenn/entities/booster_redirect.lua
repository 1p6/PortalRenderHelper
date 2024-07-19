local entity = {}
local drawableNinePatch = require("structs.drawable_nine_patch")
local drawableSprite = require("structs.drawable_sprite")

entity.name = 'PortalRenderHelper/BoosterRedirect'
entity.depth = 8995
entity.warnBelowSize = {16, 16}
entity.placements = {
    {
        name = 'default',
        data = {
            width = 16,
            height = 16,
            direction = 0.0,
        },
    }
}

function entity.rotate(room, entity, direction)
    if direction > 0 then
        entity.direction = (entity.direction + 45.0) % 360.0
    else
        entity.direction = (entity.direction - 45.0) % 360.0
    end

    return true
end

local ninepatchOptions = {
    mode = 'border',
    borderMode = 'repeat',
}

function entity.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    
    local ninepatch = drawableNinePatch.fromTexture('Flynx/PortalRenderHelper/objects/BoosterRedirect/base', ninepatchOptions, x, y, width, height)
    local arrow = drawableSprite.fromTexture('Flynx/PortalRenderHelper/objects/BoosterRedirect/arrow', {
        x = x+width/2, y = y+height/2,
        rotation = (entity.direction or 0.0) / 180.0 * math.pi
    })
    local sprites = {}
    for _, sprite in ipairs(ninepatch:getDrawableSprite()) do
        table.insert(sprites, sprite)
    end
    table.insert(sprites, arrow)
    return sprites
end

return entity
