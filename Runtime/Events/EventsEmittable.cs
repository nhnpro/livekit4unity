namespace LiveKitUnity.Runtime.Events
{
    public abstract class EventsEmittable
    {
        //TODO
        public EventEmitter events { get; set; } = new EventEmitter();
        

        public EventsListener CreateListener(bool synchronized)
        {
            return events.CreateListener(synchronized);
        }
    }
}