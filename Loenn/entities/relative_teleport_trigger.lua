local trig = {}
local utils = require('utils')
local drawing = require('utils.drawing')
local colors = require('consts.colors')

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
trig.nodeLineRenderType = 'line'

function trig.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local node = (entity.nodes or {})[1] or {x = 0, y = 0}
    local nodeX, nodeY = node.x or 0, node.y or 0
    return utils.rectangle(x, y, width, height), {
        utils.rectangle(nodeX, nodeY, width, height)
    }
end

function trig.draw(room, entity, viewport)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local lineWidth = love.graphics.getLineWidth()

    drawing.callKeepOriginalColor(function()
        love.graphics.setColor(colors.triggerBorderColor)
        love.graphics.rectangle("line", x + lineWidth / 2, y + lineWidth / 2, width - lineWidth, height - lineWidth)

        love.graphics.setColor(colors.triggerColor)
        love.graphics.rectangle("fill", x + lineWidth, y + lineWidth, width - 2 * lineWidth, height - 2 * lineWidth)

        love.graphics.setColor(colors.triggerTextColor)
        drawing.printCenteredText(humanName, x, y, width, height)
    end)
end

return trig