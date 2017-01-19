namespace FSharpHero

module SDL_FSH =

    open SDL2
    open System.Runtime.InteropServices
    open Microsoft.FSharp.NativeInterop
    open System

    type SDL_Texture = nativeint
    type SDL_Window = nativeint
    type SDL_Renderer = nativeint

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


    [<EntryPoint>]
    let main argv = 
   
        if SDL.SDL_Init(SDL.SDL_INIT_VIDEO) <> 0 then
            printfn "error on init"
        else
            printfn "success"
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
                    if SDL.SDL_WaitEvent(&event) <> 1 then 
                        quit <- true
                    else         
                        quit <- HandleEvent(event)
                RenderWeirdGradient GlobalBackBuffer xoff yoff
                UpdateWindow window renderer GlobalBackBuffer
                yoff <- yoff + 2
                xoff <- xoff + 1
                
    

    
        0 // return an integer exit code
