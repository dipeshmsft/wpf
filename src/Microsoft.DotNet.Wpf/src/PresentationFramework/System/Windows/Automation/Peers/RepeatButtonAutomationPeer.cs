// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;

namespace System.Windows.Automation.Peers
{
    /// 
    public class RepeatButtonAutomationPeer : ButtonBaseAutomationPeer, IInvokeProvider
    {
        ///
        public RepeatButtonAutomationPeer(RepeatButton owner): base(owner)
        {}
    
        ///
        override protected string GetClassNameCore()
        {
            return "RepeatButton";
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Button;
        }

        /// 
        override public object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Invoke)
            {
                return this;
            }
            else
            {
                return base.GetPattern(patternInterface);
            }
        }

        void IInvokeProvider.Invoke()
        {
            if(!IsEnabled())
                throw new ElementNotEnabledException();

            RepeatButton owner = (RepeatButton)Owner;
            owner.AutomationButtonBaseClick();
        }
    }
}

