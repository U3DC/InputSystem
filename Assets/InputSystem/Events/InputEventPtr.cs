using System;
using ISX.Utilities;

namespace ISX.LowLevel
{
    /// <summary>
    /// Pointer to an <see cref="InputEvent"/>. Makes it easier to work with InputEvents and hides
    /// the unsafe operations necessary to work with events.
    /// </summary>
    public unsafe struct InputEventPtr
    {
        // C# does not allow us to have pointers to structs that have managed data members. Since
        // this can't be guaranteed for generic type parameters, they can't be used with pointers.
        // This is why we cannot make InputEventPtr generic or have a generic method that returns
        // a pointer to a specific type of event.
        private InputEvent* m_EventPtr;

        public InputEventPtr(InputEvent* eventPtr)
        {
            m_EventPtr = eventPtr;
        }

        public InputEventPtr(IntPtr eventPtr)
            : this((InputEvent*)eventPtr)
        {
        }

        public bool valid
        {
            get { return m_EventPtr != null; }
        }

        public bool handled
        {
            get
            {
                if (!valid)
                    return false;
                return m_EventPtr->handled;
            }
            set
            {
                if (!valid)
                    throw new NullReferenceException();
                m_EventPtr->handled = value;
            }
        }

        public int id
        {
            get
            {
                if (!valid)
                    return 0;
                return m_EventPtr->eventId;
            }
        }

        public FourCC type
        {
            get
            {
                if (!valid)
                    return new FourCC();
                return m_EventPtr->type;
            }
        }

        public uint sizeInBytes
        {
            get
            {
                if (!valid)
                    return 0;
                return m_EventPtr->sizeInBytes;
            }
        }

        public int deviceId
        {
            get
            {
                if (!valid)
                    return InputDevice.kInvalidDeviceId;
                return m_EventPtr->deviceId;
            }
            set
            {
                if (!valid)
                    throw new NullReferenceException();
                m_EventPtr->deviceId = value;
            }
        }

        public double time
        {
            get { return valid ? m_EventPtr->time : 0.0; }
        }

        public IntPtr data
        {
            get { return new IntPtr(m_EventPtr); }
        }

        public bool IsA<TOtherEvent>()
            where TOtherEvent : struct, IInputEventTypeInfo
        {
            if (m_EventPtr == null)
                return false;

            var otherEventTypeCode = new TOtherEvent().GetTypeStatic();
            return m_EventPtr->type == otherEventTypeCode;
        }

        // NOTE: It is your responsibility to know *if* there actually another event following this one in memory.
        public InputEventPtr Next()
        {
            if (!valid)
                return new InputEventPtr();

            return new InputEventPtr(new IntPtr(new IntPtr(m_EventPtr).ToInt64() + sizeInBytes));
        }

        public override string ToString()
        {
            if (!valid)
                return "null";
            return m_EventPtr->ToString();
        }
    }
}
