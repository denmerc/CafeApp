module EventStore
open States
open System
open NEventStore
open Events

let getTabIdFromState = function
| ClosedTab None -> None
| OpenedTab tab -> Some tab.Id
| PlacedOrder po -> Some po.TabId
| OrderInProgress ipo -> Some ipo.PlacedOrder.TabId
| OrderServed payment -> Some payment.TabId
| ClosedTab (Some tabId) -> Some tabId

let saveEvent (storeEvents : IStoreEvents) state event  =
  match getTabIdFromState state with
  | Some tabId ->
    use stream = storeEvents.OpenStream(tabId.ToString())
    stream.Add(new EventMessage(Body = event))
    stream.CommitChanges(Guid.NewGuid())
  | _ -> ()

let getEvents (storeEvents : IStoreEvents) (tabId : Guid) =
  use stream = storeEvents.OpenStream(tabId.ToString())
  stream.CommittedEvents
  |> Seq.map (fun msg -> msg.Body)
  |> Seq.cast<Event>

let getState storeEvents tabId =
  getEvents storeEvents tabId
  |> Seq.fold apply (ClosedTab None)

type EventStore = {
  GetState : Guid -> State
  SaveEvent : State * Event -> unit
}