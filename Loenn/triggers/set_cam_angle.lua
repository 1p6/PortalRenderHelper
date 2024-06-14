local trig = {}

trig.name = 'PortalRenderHelper/SetCameraAngleTrigger'

trig.placements = {
    {
        name = "default",
        data = {
            width = 16,
            height = 16,
            flag = '',
            invert = false,
            angle = 0.0,
            setAngle = false,
            setTargetAngle = true,
            setOnEnter = true,
            setOnExit = false,
            setOnStay = false,
            setOnLoad = false,
            setOnUnload = false,
        }
    }
}

return trig
