namespace FSharpHero

module Utils =
    let PI32 = 3.14159265359f

    let EmptyButtonState = {
        HalfTransitionCount = 0
        EndedDown = false
    }

    let EmptyController = {
        Analog = false
        StickAverageX = 0.0f
        StickAverageY = 0.0f
        Buttons = 
            {
                MoveUp = EmptyButtonState
                MoveDown = EmptyButtonState
                MoveLeft = EmptyButtonState
                MoveRight = EmptyButtonState

                ActionUp= EmptyButtonState
                ActionDown = EmptyButtonState
                ActionRight = EmptyButtonState
                ActionLeft = EmptyButtonState

                LeftShoulder = EmptyButtonState
                RightShoulder = EmptyButtonState

                Start = EmptyButtonState
                Back = EmptyButtonState
            }
    }


    
    