namespace Gah

open NotebookApp

open Aardium
open Aardvark.Service
open Aardvark.UI
open Suave
open Suave.WebPart
open Aardvark.Rendering.Vulkan
open Aardvark.Base
open FSharp.Data.Adaptive
open System



module Foo = 

    [<EntryPoint>]
    let main args =
        Aardvark.Init()
        Aardium.init()
        Aardvark.Base.Report.Verbosity <- -1

        let app = new Aardvark.Application.Slim.OpenGlApplication()

        let mstate, mapp = App.app.startAndGetState()

        WebPart.startServer 4321 [
            MutableApp.toWebPart' app.Runtime false mapp
        ] |> ignore

        let mapp2 = 
            { mapp with ui = App.view2 mstate }

        WebPart.startServer 4322 [
            MutableApp.toWebPart' app.Runtime false mapp2
        ] |> ignore
    
        Console.ReadLine() |> ignore

        0
