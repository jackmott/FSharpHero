namespace FSharpHero

   

   type OffscreenBuffer = {
        mutable Memory : int32[]            
        mutable Width : int32
        mutable Height : int32
        mutable Pitch : int
    }

    type SoundOutputBuffer = {
        Samples : int16 []
        SampleCount: int
        SamplesPerSecond:int
    }
    
    
    type ButtonState = {
        mutable HalfTransitionCount:int
        mutable EndedDown: bool
    }

   

    type ButtonRecord = {
        MoveUp : ButtonState
        MoveDown : ButtonState
        MoveLeft : ButtonState
        MoveRight : ButtonState

        ActionUp: ButtonState
        ActionDown : ButtonState
        ActionRight : ButtonState
        ActionLeft : ButtonState

        LeftShoulder : ButtonState
        RightShoulder : ButtonState

        Start : ButtonState
        Back : ButtonState
    }
    
        
    type ControllerInput = {
        mutable Analog:bool
        mutable StickAverageX:float32
        mutable StickAverageY:float32
        mutable Buttons : ButtonRecord
    }

   

    type GameInput = {
        Controllers:ControllerInput[]
    }

    type GameState = {
        mutable ToneHz: int
        mutable ToneSine : float32
        mutable GreenOffset: int
        mutable BlueOffset: int
    }

