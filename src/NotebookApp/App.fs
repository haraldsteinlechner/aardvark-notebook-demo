namespace NotebookApp

open System
open Aardvark.Base
open Aardvark.SceneGraph
open Aardvark.UI
open Aardvark.UI.Primitives
open Aardvark.Base.Rendering
open FSharp.Data.Adaptive
open NotebookApp.Model

type Message =
    | ToggleModel
    | CameraMessage of FreeFlyController.Message
    | Inc
    | AddAnnotation of V3d
    | ClearAnnotations

module App =
    
    let initial = { currentModel = Box; cameraState = FreeFlyController.initial; cnt = 0; annotations = IndexList.empty }

    let update (m : Model) (msg : Message) =
        match msg with
            | ToggleModel -> 
                match m.currentModel with
                    | Box -> { m with currentModel = Sphere }
                    | Sphere -> { m with currentModel = Box }

            | CameraMessage msg ->
                { m with cameraState = FreeFlyController.update m.cameraState msg }

            | Inc -> { m with cnt = m.cnt + 1 }
            | AddAnnotation p -> { m with annotations = IndexList.add p m.annotations }
            | ClearAnnotations -> { m with annotations = IndexList.empty }

    let view (m : AdaptiveModel) =

        let frustum = 
            Frustum.perspective 60.0 0.1 100.0 1.0 
                |> AVal.constant


        let annotations = 
            m.annotations |> AList.toASet |> ASet.map (fun p -> 
                Sg.sphere' 2 C4b.White 0.01 |> Sg.noEvents |> Sg.translate p.X p.Y p.Z
            ) |> Sg.set

        let sg =
            m.currentModel |> AVal.map (fun v ->
                match v with
                    | Box -> Sg.box (AVal.constant C4b.Red) (AVal.constant (Box3d(-V3d.III, V3d.III)))
                    | Sphere -> 
                        Sg.sphere 5 (AVal.constant C4b.Green) (AVal.constant 1.0)
                        |> Sg.requirePicking
                        |> Sg.noEvents
                        |> Sg.withEvents [
                                Sg.onDoubleClick AddAnnotation
                           ]
            )
            |> Sg.dynamic
            |> Sg.andAlso annotations
            |> Sg.shader {
                do! DefaultSurfaces.trafo
                do! DefaultSurfaces.simpleLighting
            }


        body [] [
            FreeFlyController.controlledControl m.cameraState CameraMessage frustum (AttributeMap.ofList [style "position: fixed; left: 0; top: 0; width: 100%; height: 100%"; attribute "data-samples" "8"]) sg

            div [style "position: fixed; left: 20px; top: 20px"] [
                button [onClick (fun _ -> ToggleModel)] [text "Toggle Model"]

                Incremental.text (AVal.map string m.cnt)
            ]
        ]

    let viewAnnotations (m : AdaptiveModel) =
        body [] [
            require Html.semui (
                Incremental.div (AttributeMap.ofList [clazz "ui"; style "position: fixed; left: 20px; top: 20px"]) <|
                    alist {
                        for p in m.annotations do
                            yield div [clazz "ui label"] [text (sprintf "%A" p)]
                            yield br []
               
                }
            )
        ]

    let app =
        {
            initial = initial
            update = update
            view = view
            threads = fun m -> m.cameraState |> FreeFlyController.threads |> ThreadPool.map CameraMessage
            unpersist = Unpersist.instance
        }