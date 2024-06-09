local drawableLine = require('structs.drawable_line')
local drawing = require('utils.drawing')
local utils = require('utils')
local ent = {}
local fromColor = 'red'
local toColor = 'green'
local arrowColor = 'blue'

ent.name = 'PortalRenderHelper/PortalRenderPoly'

ent.nodeLimits = {2, -1}
-- ent.nodeLineRenderType = 'line'
ent.nodeVisibility = 'never'
-- not having this breaks line sprite rendering????
ent.depth = -10000
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
    entity = entity or {}
    local nodes = {}
    for i,node in ipairs(entity.nodes or {}) do
        local nodeX, nodeY = node.x or 0, node.y or 0
        nodes[i] = utils.rectangle(nodeX-4, nodeY-4, 8, 8)
    end
    local entityX, entityY = entity.x or 0, entity.y or 0
    return utils.rectangle(entityX-4, entityY-4, 8, 8), nodes
end

local function arrowSprites(fromX, fromY, toX, toY, color)
    local deltaX = fromX - toX
    local deltaY = fromY - toY
    local len = math.sqrt(deltaX*deltaX + deltaY*deltaY)
    if len < 0.01 then return {} end
    len = 2*math.sqrt(2) / len
    deltaX = deltaX * len
    deltaY = deltaY * len
    return {
        drawableLine.fromPoints(
            {
                fromX, fromY, toX, toY
            }, color
        ),
        drawableLine.fromPoints(
            {
                toX + deltaX - deltaY, toY + deltaY + deltaX,
                toX, toY,
                toX + deltaX + deltaY, toY + deltaY - deltaX,
            }, color
        )
    }
end

function ent.sprite(room, entity, viewport)
    entity = entity or {}
    local entityX, entityY = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {}
    local destNode = nodes[1] or {}
    local destX, destY = destNode.x or 0, destNode.y or 0
    local lines = {
        arrowSprites(destX, destY, entityX, entityY, 'blue')
    }
    local offsetX = destX - entityX
    local offsetY = destY - entityY
    local prevX, prevY = entityX, entityY
    for i,node in ipairs(nodes) do
        if i == 1 then goto continue end
        local nodeX, nodeY = node.x or 0, node.y or 0
        lines[#lines+1] = drawableLine.fromPoints(
            {prevX, prevY, nodeX, nodeY},
            'red'
        )
        lines[#lines+1] = drawableLine.fromPoints(
            {prevX+offsetX, prevY+offsetY, nodeX+offsetX, nodeY+offsetY},
            'green'
        )
        prevX, prevY = nodeX, nodeY
        ::continue::
    end
    if entity.closed then
        -- since it's closed, making these arrows is the only way to tell the orientation of the polygon. with open polygons, which end the blue arrow connects to gives the orientation
        lines[#lines+1] = arrowSprites(prevX, prevY, entityX, entityY, 'red')
        lines[#lines+1] = arrowSprites(prevX+offsetX, prevY+offsetY, entityX+offsetX, entityY+offsetY, 'green')
    end
    return table.flatten(lines)
end

return ent
