﻿namespace Examples

open System
open System.Collections.Generic

open FsCheck
open FsCheck.Xunit



module ``4 CSharp`` =

    // Creates an IWidget using an object expression
    let widget name size = {
        new Object() with
            override x.ToString() = sprintf "(%s, %d)" name size
        interface IWidget with
            member x.Name = name
            member x.Size = size }

    // Creates an IWidgetMaker
    let widgetMaker (widgets: IWidget list) =
        let stack = Stack (widgets |> List.rev)
        { new IWidgetMaker with 
            member x.MakeWidget() = stack.Pop()
            member x.CanMake = stack.Count > 0 }

    // Composition of widgetMaker and WidgetProducer
    let widgetProducer = widgetMaker >> WidgetProducer

    // And finally a function that we can use in our tests
    let produceWidgets fn xs = (widgetProducer xs).ProduceWidgets (fun x -> fn x) |> Seq.toList

    // We need to be able to generate an IWidget and this time we'll use the `gen` computation expression 
    let genWidget = gen {
        let! name = Arb.generate<string>
        let! size = Gen.choose(0, 10) // but have a boundary for size to make testing easier
        return widget name size
    }

    type Widgets =
        static member Widgets () =
            genWidget
            |> Gen.listOf
            |> Arb.fromGen

    [<Property(Verbose=true, Arbitrary=[| typeof<Widgets> |])>]
    let ``WidgetProducer produces and filters widgets`` (widgets: IWidget list) =
        // lets create a few helpers
        let produceAll = produceWidgets (fun _ -> true)
        let produceNone = produceWidgets (fun _ -> false)
        let produceSome = produceWidgets (fun x -> x.Size > 3 && x.Size < 9)
        let isEqual (w1:IWidget) (w2:IWidget) = w1.Name = w2.Name && w1.Size = w2.Size
        let invalid = widget "invalid" -1
        let valid = widget "valid" 5
        
        false