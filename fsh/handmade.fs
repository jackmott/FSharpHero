namespace FSharpHero

module Handmade = 
    open System
    open Utils

    let GameOutputSound (soundBuffer:SoundOutputBuffer) (toneHz:int) (tSine:float32)= 
        let mutable mutTSine = tSine
        let toneVolume : float32 = 3000.0f
        let samplesOut = soundBuffer.Samples
        let wavePeriod : float32 = (float32)(soundBuffer.SamplesPerSecond / toneHz)

        for i in 0 .. soundBuffer.SampleCount-1 do
            let sineValue:float32 = (float32)(Math.Sin((float)mutTSine))
            let sampleValue : int16 = (int16)(sineValue*toneVolume)
            let index = i*2
            samplesOut.[index] <- sampleValue
            samplesOut.[index+1] <- sampleValue
            mutTSine <- mutTSine + 2.0f*PI32 / wavePeriod

            
        
    let RenderWeirdGradient (buffer:OffscreenBuffer) (xOffset:int32) (yOffset:int32) =        
        let mutable row = 0
        let w = buffer.Width
        let h = buffer.Height
        for y in 0 .. h-1 do
            for x in 0 .. w-1 do
                let b = (x + xOffset) % 256
                let g = (y + yOffset) % 256
                buffer.Memory.[row+x] <- int32(g <<< 8 ||| b)
            row <- row+w

    let GameState = 
        {
            ToneHz = 261
            ToneSine = 0.0f
            BlueOffset = 0
            GreenOffset = 0
        }
    
    let GameUpdateAndRender (input:GameInput) 
                            (buffer:OffscreenBuffer) 
                            (soundBuffer:SoundOutputBuffer) =
        

        //  for(int i = 0; i < ArrayCount(Input->Controllers); i++){
        //      game_controller_input *controller = &Input->Controllers[i];
        for controller in input.Controllers do
            if controller.Analog then
                GameState.ToneHz <- 256 + (int)(128.0f * controller.StickAverageY)
                GameState.BlueOffset <- GameState.BlueOffset + (int)(4.0f*controller.StickAverageX)
            else 
                if controller.Buttons.MoveLeft.EndedDown then
                    GameState.BlueOffset <- GameState.BlueOffset - 1
                else if controller.Buttons.MoveRight.EndedDown then
                    //GameState->Blueoffset += 1
                    GameState.BlueOffset <- GameState.BlueOffset + 1

            if controller.Buttons.ActionDown.EndedDown then
                GameState.GreenOffset <- GameState.GreenOffset + 1

        GameOutputSound soundBuffer GameState.ToneHz GameState.ToneSine
        RenderWeirdGradient buffer GameState.BlueOffset GameState.GreenOffset