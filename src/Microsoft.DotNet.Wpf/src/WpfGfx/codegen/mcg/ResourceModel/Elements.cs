// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//---------------------------------------------------------------------------
//

//
// Description: This is a place to add helper methods to the data model
//              generated by xsd.exe
//
//              See also Generated\Elements.cs
//              

using System;

namespace MS.Internal.MilCodeGen.ResourceModel
{
    public partial class CGEvent
    {
        public string Owner
        {
            get { return Name.Split('.')[0]; }
        }

        public string ClrEventName
        {
            get { return Name.Split('.')[1]; }
        }
        
        public string RoutedEventName
        {
            get { return Name + "Event"; }
        }

        public string AliasedRoutedEventName
        {
            get { return RoutedEventName.Split('.')[1]; }
        }

        public string ThunkName
        {
            get { return "On" + ClrEventName + "Thunk"; }
        }

        public string ArgsType
        {
            get
            {
                string suffix = "Handler";

                if (HandlerType.EndsWith(suffix))
                {
                    // This is for handlers like MouseButtonEventHandler
                    return HandlerType.Remove(HandlerType.Length - suffix.Length, suffix.Length) + "Args";
                }
                else if ((HandlerType.Length > 2) && (HandlerType[HandlerType.Length - 1] == '>'))
                {
                    // This is for handlers like EventArgs<TouchEventArgs>
                    int index = HandlerType.IndexOf(suffix + "<");
                    if (index > 0)
                    {
                        index += suffix.Length + 1;
                        return HandlerType.Substring(index, HandlerType.Length - index - 1);
                    }
                }
                
                throw new Exception(String.Format("Name of HandlerType '{0}' expected to end with 'Handler' or 'Handler<EventArgType>'.", HandlerType));
            }
        }

        public string VirtualName
        {
            get
            {
                // The routed event names are specified in the from "DragDrop.DropPreview".
                // We split off the owning type so we can create the virtual "OnDropPreview".
                string vName = ClrEventName;

                // Remove the "Preview" prefix for Commanding events.  Command Manager will
                // determine if preview or regular event.
                string prefix = "Preview";
                if (Commanding && vName.StartsWith(prefix))
                {
                    vName = vName.Remove( /* startIndex = */ 0, prefix.Length);
                }
                
                return "On" + vName;
            }
        }
    }

    public partial class CGProperty
    {
        public string Owner
        {
            get { return Name.Split('.')[0]; }
        }

        public string PropertyName
        {
            get { return Name.Split('.')[1]; }
        }
    }
}



