local trig = {}
local utils = require('utils')
local drawing = require('utils.drawing')
local colors = require('consts.colors')
local drawableRectangle = require('structs.drawable_rectangle')
local drawableFunc = require('structs.drawable_function')
local drawableSprite = require('structs.drawable_sprite')

trig.name = "PortalRenderHelper/RelativeTeleportTrigger"
local humanName = utils.humanizeVariableName(trig.name)

trig.placements = {
    {
        name = "default",
        data = {
            width = 16,
            height = 16,
            flag = '',
            invert = false,
        }
    }
}

trig.nodeLimits = {1,1}
trig.depth = 0
trig.nodeLineRenderType = 'line'
trig.nodeVisibility = 'always'

function trig.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local node = (entity.nodes or {})[1] or {x = 0, y = 0}
    local nodeX, nodeY = node.x or 0, node.y or 0
    return utils.rectangle(x, y, width, height), {
        utils.rectangle(nodeX, nodeY, width, height)
    }
end

function trig.sprite(room, entity, viewport)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    return {
        drawableRectangle.fromRectangle('bordered', x, y, width, height, colors.triggerColor, colors.triggerBorderColor),
        -- todo, in dev branch of loenn, drawable text exists. use that instead when that comes out mayhaps
        drawableSprite.fromTexture('Flynx/PortalRenderHelper/objects/teleport', {
            x = x + width/2, y = y + height/2,
            depth = trig.depth
        }),
    }
end

function trig.nodeSprite(room, entity, node, nodeIndex, viewport)
    local x, y = node.x or 0, node.y or 0
    local width, height = entity.width or 16, entity.height or 16
    return {
        drawableRectangle.fromRectangle('bordered', x, y, width, height, colors.triggerColor, colors.triggerBorderColor),
        drawableSprite.fromTexture('Flynx/PortalRenderHelper/objects/teleport2', {
            x = x + width/2, y = y + height/2,
            depth = trig.depth
        }),
    }
end

return trig