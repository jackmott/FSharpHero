namespace FSharpHero

module SDL_FSH =

    open SDL2
    open System.Runtime.InteropServices
    open Microsoft.FSharp.NativeInterop
    open System

    type SDL_Texture = nativeint
    type SDL_Window = nativeint
    type SDL_Renderer = nativeint
    type SDL_GameController = nativeint
    type SDL_Haptic = nativeint

    let MAX_CONTROLLERS = 4
    type Controller =
      {
        Controller : SDL_GameController
        Rumbler : SDL_Haptic
      }

    let Controllers = ResizeArray<Controller>(1)
    

    type SDL_OffscreenBuffer = 
        {
            mutable Memory : int32[]
            mutable Texture : SDL_Texture
            mutable Width : int32
            mutable Height : int32
            mutable Pitch : int
        }

    type SDL_WindowSize =
        {
            Width : int
            Height : int
        }
    
    let GlobalBackBuffer =
        {
            Memory = Array.zeroCreate<int32> 0
            Texture = IntPtr.Zero
            Width = 0
            Height = 0
            Pitch = 0
        }

    let GetWindowSize (window : SDL_Window) = 
        let mutable w = 0
        let mutable h = 0
        SDL.SDL_GetWindowSize(window,&w,&h)
        {
            Width = w
            Height = h
        }

    
    let RenderWeirdGradient (buffer:SDL_OffscreenBuffer) (bOffset:int32) (gOffset:int32) =        
        let mutable row = 0
        let w = buffer.Width
        let h = buffer.Height
        for y in 0 .. h-1 do
            for x in 0 .. w-1 do
                let b = (x + bOffset) % 256
                let g = (y + gOffset) % 256
                buffer.Memory.[row+x] <- int32(g <<< 8 ||| b)
            row <- row+w


    let ResizeTexture (buffer : SDL_OffscreenBuffer) (renderer : SDL_Renderer) (w:int) (h:int) =        
                        
        if buffer.Texture <> IntPtr.Zero then
            SDL.SDL_DestroyTexture(buffer.Texture) |> ignore        
        buffer.Memory <- Array.zeroCreate (w*h)
        buffer.Texture <- SDL.SDL_CreateTexture(renderer,
                                                SDL.SDL_PIXELFORMAT_ARGB8888,
                                                (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
                                                w,h)
        printfn "%A" (SDL.SDL_GetError())
        buffer.Width <- w
        buffer.Height <- h
        buffer.Pitch <- 4*w
        
        

    let UpdateWindow (window:SDL_Window) (renderer:SDL_Renderer) (buffer:SDL_OffscreenBuffer) =
        let pixelHandle = GCHandle.Alloc(buffer.Memory,GCHandleType.Pinned)
        let pixelPtr = pixelHandle.AddrOfPinnedObject()                
        
        if SDL.SDL_UpdateTexture(buffer.Texture,
                                 IntPtr.Zero,pixelPtr,
                                 buffer.Pitch) <> 0 then
            printfn "UpdateTexture Error"
                
        if SDL.SDL_RenderCopy(renderer,buffer.Texture,IntPtr.Zero,IntPtr.Zero) <> 0 then
            printfn "RenderCopy Error"

        SDL.SDL_RenderPresent(renderer)
        pixelHandle.Free();

    let HandleEvent (event:SDL.SDL_Event) =
        
        match event.``type`` with
        | SDL.SDL_EventType.SDL_QUIT  
            -> printfn "Quit Event"
               true
        | SDL.SDL_EventType.SDL_KEYDOWN
        | SDL.SDL_EventType.SDL_KEYUP
            -> let keyCode = event.key.keysym.sym
               let isDown = event.key.state = SDL.SDL_PRESSED
               let wasDown = 
                    if event.key.state = SDL.SDL_RELEASED || event.key.repeat <> 0uy then
                        true
                    else
                        false
               if event.key.repeat = 0uy then
                   match keyCode with
                   | SDL.SDL_Keycode.SDLK_w -> ()
                   | SDL.SDL_Keycode.SDLK_a -> ()
                   | SDL.SDL_Keycode.SDLK_s -> ()
                   | SDL.SDL_Keycode.SDLK_d -> ()
                   | SDL.SDL_Keycode.SDLK_q -> ()
                   | SDL.SDL_Keycode.SDLK_e -> ()
                   | SDL.SDL_Keycode.SDLK_UP -> ()
                   | SDL.SDL_Keycode.SDLK_LEFT -> ()
                   | SDL.SDL_Keycode.SDLK_DOWN -> ()
                   | SDL.SDL_Keycode.SDLK_RIGHT -> ()
                   | SDL.SDL_Keycode.SDLK_ESCAPE -> ()
                   | SDL.SDL_Keycode.SDLK_SPACE -> ()
                   | _ -> ()
               false

        | SDL.SDL_EventType.SDL_WINDOWEVENT 
            -> printfn "Window Event"
               match event.window.windowEvent with
               | SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED               
                    -> false
               | SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED
                    -> false
               | SDL.SDL_WindowEventID.SDL_WINDOWEVENT_EXPOSED            
                    -> let window = SDL.SDL_GetWindowFromID(event.window.windowID)
                       let renderer = SDL.SDL_GetRenderer(window)
                       UpdateWindow window renderer GlobalBackBuffer
                       false
               | _  -> false
        | _ -> false


    let OpenGameControllers () = 
        let maxJoysticks = Math.Min(SDL.SDL_NumJoysticks(), MAX_CONTROLLERS)
        for i in 0 .. maxJoysticks-1 do
            if SDL.SDL_IsGameController(i) = SDL.SDL_bool.SDL_TRUE then
                let controller = SDL.SDL_GameControllerOpen(i)
                let rumble = SDL.SDL_HapticOpen(i)
                if SDL.SDL_HapticRumbleInit(rumble) <> 0 then
                    SDL.SDL_HapticClose(rumble)
                    Controllers.Add( 
                        {
                            Controller = controller
                            Rumbler = IntPtr.Zero
                        })
                else
                    Controllers.Add(
                        {
                            Controller = controller
                            Rumbler = rumble
                        })


    let CloseGameControllers () =
        for controller in Controllers do
            SDL.SDL_GameControllerClose(controller.Controller)
            SDL.SDL_HapticClose(controller.Rumbler)        
            
    [<EntryPoint>]
    let main argv = 
        SDL.SDL_SetHint(SDL.SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1") |> ignore
        if SDL.SDL_Init(SDL.SDL_INIT_VIDEO ||| SDL.SDL_INIT_GAMECONTROLLER ||| SDL.SDL_INIT_HAPTIC) <> 0 then
            printfn "error on init"
        else            
            printfn "success"
            OpenGameControllers ()
            let window =
                SDL.SDL_CreateWindow("FSharp Hero",
                                     100,100,1920,1200,
                                     SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE)
        
            let renderer = SDL.SDL_CreateRenderer(window,-1,SDL.SDL_RendererFlags.SDL_RENDERER_SOFTWARE) 
            let windowSize = GetWindowSize window           
            ResizeTexture GlobalBackBuffer renderer windowSize.Width windowSize.Height
            let mutable quit = false;
            let mutable xoff = 0;
            let mutable yoff = 0;
            while not quit do
                let mutable event = Unchecked.defaultof<SDL.SDL_Event>
                while SDL.SDL_PollEvent(&event) <> 0 do                    
                        quit <- HandleEvent(event)

                for controllerPair in Controllers do
                    let controller = controllerPair.Controller
                    let rumbler = controllerPair.Rumbler
                    if SDL.SDL_GameControllerGetAttached(controller) = SDL.SDL_bool.SDL_TRUE then
                        let up = SDL.SDL_GameControllerGetButton(controller,SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP) <> 0uy
                        let down = SDL.SDL_GameControllerGetButton(controller,SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN) <> 0uy
                        let left = SDL.SDL_GameControllerGetButton(controller,SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT) <> 0uy
                        let right = SDL.SDL_GameControllerGetButton(controller,SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT) <> 0uy
                        let start = SDL.SDL_GameControllerGetButton(controller,SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START) <> 0uy
                        let back = SDL.SDL_GameControllerGetButton(controller,SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK) <> 0uy
                        let leftShoulder = SDL.SDL_GameControllerGetButton(controller,SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER) <> 0uy
                        let rightShoulder = SDL.SDL_GameControllerGetButton(controller,SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER) <> 0uy
                        let AButton = SDL.SDL_GameControllerGetButton(controller,SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A) <> 0uy
                        let BButton = SDL.SDL_GameControllerGetButton(controller,SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B) <> 0uy
                        let XButton = SDL.SDL_GameControllerGetButton(controller,SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X) <> 0uy
                        let YButton = SDL.SDL_GameControllerGetButton(controller,SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y) <> 0uy

                        let stickX = SDL.SDL_GameControllerGetAxis(controller,SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX)
                        let stickY = SDL.SDL_GameControllerGetAxis(controller,SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY)

                        if AButton then yoff <- yoff + 2
                        if BButton then
                            if rumbler <> IntPtr.Zero then
                                SDL.SDL_HapticRumblePlay(rumbler,0.5f,2000u) |> ignore




                RenderWeirdGradient GlobalBackBuffer xoff yoff
                UpdateWindow window renderer GlobalBackBuffer
                
                
    

        CloseGameControllers()
        SDL.SDL_Quit()
        0 // return an integer exit code
