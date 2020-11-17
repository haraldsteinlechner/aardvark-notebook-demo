namespace Aardvark.Notebooks


open System
open System.Net
open System.IO

[<AutoOpen>]
module NotebookUtilities =

    let getFreePort() =
        let l = System.Net.Sockets.TcpListener(Net.IPAddress.Loopback, 0)
        l.Start()
        let ep = l.LocalEndpoint |> unbox<Net.IPEndPoint>
        l.Stop()
        ep.Port


    open System.Threading
    open System.Collections.Concurrent
    open System.Collections.Generic
    open Aardvark.UI
    open Aardvark.Base
    open Aardvark.Base.Rendering
    open Suave
    open Suave.WebPart

    let muted (f : 'a -> 'b) =
        fun a -> 
            let o = System.Console.Out
            System.Console.SetOut(System.IO.TextWriter.Null)
            let r = f a
            System.Console.SetOut o
            r
        

    [<StructuredFormatDisplay("{AsString}")>]
    type NotebookView(port : int, url : string, dispose : unit -> unit) =
        member x.Url = url
        member x.Port = port
        member x.Dispose() = dispose()
        override x.ToString() = url
        member x.AsString = x.ToString()



    type NotebookApp<'model,'mmodel,'msg>(runtime : IRuntime, app : App<'model,'mmodel,'msg>) =
        let mstate, mapp = app.startAndGetState()
        let instances = Dictionary<int,_>()

        member x.AddView(view : 'mmodel -> DomNode<'msg>, ?port : int) = 
            let port, update = 
                match port with
                | None -> getFreePort(), false
                | Some p -> p, true

            match instances.TryGetValue port with
            | (true,v) -> 
                if update then
                    Log.line "overriding notebook app with port: %d" port
                    instances.Remove port |> ignore
                else failwith "port collision"
            | _ -> ()

            let mapp = 
                { mapp with ui = view mstate }

            let d = 
                muted (fun _  -> 
                    let r = 
                        lock mapp.lock (fun _ -> 
                            WebPart.startServer port [
                                MutableApp.toWebPart' runtime false mapp
                            ] 
                        )
                    System.Threading.Thread.Sleep(100)
                    r
                ) ()
            let url = sprintf "http://localhost:%d" port
            let view = NotebookView(port, url, fun _ -> x.RemoveView port)
            instances.Add(port, d)
            view

        
        member x.RemoveView(port : int) =
            match instances.TryGetValue(port) with
            | (true,v) -> 
                v.Dispose()
                instances.Remove port |> ignore
            | _ -> ()

        member x.RemoveView(view : NotebookView) =
            match instances.TryGetValue(view.Port) with
            | (true,v) -> 
                v.Dispose()
                instances.Remove view.Port |> ignore
            | _ -> ()

        member x.Update(msgs: seq<'msg>) =
            mapp.update Guid.Empty msgs

        member x.State = mstate

    type private Self = Self

    module Notebooks =
        open Aardvark.Base

        let init () =
            Aardvark.Base.IntrospectionProperties.CustomEntryAssembly <- typeof<Self>.Assembly
            muted (fun _ -> 
                Aardvark.Init()
                Aardvark.Base.Report.Verbosity <- -1
            ) ()