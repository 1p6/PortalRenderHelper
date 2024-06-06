local drawableLine = require('structs.drawable_line')
local drawing = require('utils.drawing')
local utils = require('utils')
local ent = {}

ent.name = 'PortalRenderHelper/PortalRenderPoly'

ent.nodeLimits = {2, -1}
-- ent.nodeLineRenderType = 'line'
ent.nodeVisibility = 'never'
-- ent.texture = 'objects/refill/idle00'
ent.placements = {
    {
        name = 'default',
        data = {
            closed = false,
            flag = '',
            invert = false,
        },
    }
}
function ent.selection(room, entity)
    local nodes = {}
    for i,node in ipairs(entity.nodes) do
        nodes[i] = utils.rectangle(node.x-4, node.y-4, 8, 8)
    end
    return utils.rectangle(entity.x-4, entity.y-4, 8, 8), nodes
end
-- function ent.sprite(room, entity, viewport)
--     local lines = {drawableLine.fromPoints(
--         {entity.x, entity.y, entity.nodes[1].x, entity.nodes[1].y},
--         'red' -- red color
--     )}
--     return lines
-- end
function ent.draw(room, entity, viewport)
    drawing.callKeepOriginalColor(function()
        love.graphics.setColor(0, 0, 1)
        love.graphics.line(entity.x, entity.y, entity.nodes[1].x, entity.nodes[1].y)
        love.graphics.setColor(1, 0, 0)
        local line = {entity.x, entity.y}
        for i,node in ipairs(entity.nodes) do
            if i ~= 1 then
                line[2*i-1] = node.x
                line[2*i] = node.y
            end
        end
        if entity.closed then
            line[#line + 1] = entity.x
            line[#line + 1] = entity.y
        end
        love.graphics.line(line)
        love.graphics.setColor(0, 1, 0)
        local offsetX = entity.nodes[1].x - entity.x
        local offsetY = entity.nodes[1].y - entity.y
        for i = 1, (#line)/2 do
            line[2*i-1] = line[2*i-1] + offsetX
            line[2*i] = line[2*i] + offsetY
        end
        love.graphics.line(line)
    end)
end

return ent
