namespace FSharpHero

module SDL_FSH =

    open SDL2
    open System.Runtime.InteropServices
    open Microsoft.FSharp.NativeInterop
    open System

    let mutable pixels = Array.zeroCreate<int32> 0
    let mutable texture = Unchecked.defaultof<nativeint>
    let mutable textureWidth = Unchecked.defaultof<int>
    let mutable textureHeight = Unchecked.defaultof<int>

    let RenderWeirdGradient (boffset:int32) (goffset:int32) =        
        let mutable row = 0
        for y in 0 .. textureHeight-1 do
            for x in 0 .. textureWidth-1 do
                let b = (x + boffset) % 256
                let g = (y + goffset) % 256
                pixels.[row+x] <- int32(g <<< 8 ||| b)
            row <- row+textureWidth


    let CreateTexture (window:nativeint) =        
        let mutable Width = Unchecked.defaultof<int>
        let mutable Height = Unchecked.defaultof<int>
        SDL.SDL_GetWindowSize(window,&Width,&Height)
        let renderer = SDL.SDL_GetRenderer(window)
        if texture <> Unchecked.defaultof<nativeint> then
            SDL.SDL_DestroyTexture(texture) |> ignore
        texture <- SDL.SDL_CreateTexture(renderer,
                                         SDL.SDL_PIXELFORMAT_ARGB8888,
                                         (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
                                         Width,
                                         Height)
        pixels <- Array.zeroCreate (Width*Height)
        textureWidth <- Width
        textureHeight <- Height

    let UpdateWindow (window:nativeint) (renderer:nativeint) =
        let pixelHandle = GCHandle.Alloc(pixels,GCHandleType.Pinned)
        let pixelPtr = pixelHandle.AddrOfPinnedObject()                
        
        if SDL.SDL_UpdateTexture(texture,IntPtr.Zero,pixelPtr,textureWidth*4) <> 0 then
            printfn "UpdateTexture Error"
                
        if SDL.SDL_RenderCopy(renderer,texture,IntPtr.Zero,IntPtr.Zero) <> 0 then
            printfn "RenderCopy Error"

        SDL.SDL_RenderPresent(renderer)
        pixelHandle.Free();

    let HandleEvent (event:SDL.SDL_Event) =
        
        match event.``type`` with
        | SDL.SDL_EventType.SDL_QUIT  
            -> printfn "Quit Event"
               true
        | SDL.SDL_EventType.SDL_WINDOWEVENT 
            -> printfn "Window Event"
               match event.window.windowEvent with
               | SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED               
                    -> let window = SDL.SDL_GetWindowFromID(event.window.windowID)
                       let renderer = SDL.SDL_GetRenderer(window)
                       CreateTexture window 
                       false
               | SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED
                    -> false
               | SDL.SDL_WindowEventID.SDL_WINDOWEVENT_EXPOSED            
                    -> let window = SDL.SDL_GetWindowFromID(event.window.windowID)
                       let renderer = SDL.SDL_GetRenderer(window)
                       UpdateWindow window renderer
                       false
               | _  -> false
        | _ -> false


    [<EntryPoint>]
    let main argv = 
   
        if SDL.SDL_Init(SDL.SDL_INIT_VIDEO) <> 0 then
            printfn "error on init"
        else
            printfn "success"
            let windowHandle =
                SDL.SDL_CreateWindow("FSharp Hero",
                                        100,100,1920,1200,
                                        SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE)
        
            let renderer = SDL.SDL_CreateRenderer(windowHandle,-1,SDL.SDL_RendererFlags.SDL_RENDERER_SOFTWARE) 
            CreateTexture windowHandle
            let mutable quit = false;
            let mutable xoff = 0;
            let mutable yoff = 0;
            while not quit do
                let mutable event = Unchecked.defaultof<SDL.SDL_Event>
                while SDL.SDL_PollEvent(&event) <> 0 do
                    if SDL.SDL_WaitEvent(&event) <> 1 then 
                        quit <- true
                    else         
                        quit <- HandleEvent(event)
                RenderWeirdGradient xoff yoff
                UpdateWindow windowHandle renderer
                yoff <- yoff + 2
                xoff <- xoff + 1
                
    

    
        0 // return an integer exit code
