namespace NotebookApp.Model

open System
open Aardvark.Base
open Aardvark.UI.Primitives
open FSharp.Data.Adaptive
open Adaptify

type Primitive =
    | Box
    | Sphere


[<ModelType>]
type Model =
    {
        currentModel    : Primitive
        cameraState     : CameraControllerState
        cnt : int

        annotations     : IndexList<V3d>
    }