// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// 
//
// Description: Value pattern provider wrapper for WCP
//
//

using System.Windows.Threading;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation
{
    // Automation/WCP Wrapper class: Implements that UIAutomation I...Provider
    // interface, and calls through to a WCP AutomationPeer which implements the corresponding
    // I...Provider inteface. Marshalls the call from the RPC thread onto the
    // target AutomationPeer's context.
    //
    // Class has two major parts to it:
    // * Implementation of the I...Provider, which uses Dispatcher.Invoke
    //   to call a private method (lives in second half of the class) via a delegate,
    //   if necessary, packages any params into an object param. Return type of Invoke
    //   must be cast from object to appropriate type.
    // * private methods - one for each interface entry point - which get called back
    //   on the right context. These call through to the peer that's actually
    //   implenting the I...Provider version of the interface. 
    internal class ValueProviderWrapper: MarshalByRefObject, IValueProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private ValueProviderWrapper( AutomationPeer peer, IValueProvider iface )
        {
            _peer = peer;
            _iface = iface;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Interface IValueProvider
        //
        //------------------------------------------------------
 
        #region Interface IValueProvider

        public void SetValue( string val )
        {
            ElementUtil.Invoke( _peer, new DispatcherOperationCallback( SetValueInternal ), val );
        }

        public string Value
        {
            get
            {
                return (string) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetValue ), null );
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return (bool) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetIsReadOnly ), null );
            }
        }

        #endregion Interface IValueProvider


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal static object Wrap( AutomationPeer peer, object iface )
        {
            return new ValueProviderWrapper( peer, (IValueProvider) iface );
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        private object SetValueInternal( object arg )
        {
            _iface.SetValue( (string)arg );
            return null;
        }

        private object GetValue( object unused )
        {
            return _iface.Value;
        }

        private object GetIsReadOnly( object unused )
        {
            return _iface.IsReadOnly;
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationPeer _peer;
        private IValueProvider _iface;

        #endregion Private Fields
    }
}
