﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace System.Windows.Automation.Peers
{
    /// 
    public class ListBoxItemAutomationPeer : SelectorItemAutomationPeer, IScrollItemProvider
    {
        ///
        public ListBoxItemAutomationPeer(object owner, SelectorAutomationPeer selectorAutomationPeer)
            : base(owner, selectorAutomationPeer)
        {
        }

        ///
        override protected string GetClassNameCore()
        {
            return "ListBoxItem";
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ListItem;
        }

        ///
        override public object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.ScrollItem)
            {
                return this;
            }
            return base.GetPattern(patternInterface);
        }

        ///For ComboBoxItem for which peer is this element only, scrolling should happen when the pop-up is expanded.
        internal override void RealizeCore()
        {
            ComboBox parent = ItemsControlAutomationPeer.Owner as ComboBox;
            if (parent != null)
            {
                IExpandCollapseProvider iecp = (IExpandCollapseProvider)UIElementAutomationPeer.FromElement(parent) as ComboBoxAutomationPeer;
                if (iecp.ExpandCollapseState != ExpandCollapseState.Expanded)
                    iecp.Expand();
            }
            base.RealizeCore();
        }

        void IScrollItemProvider.ScrollIntoView()
        {
            ListBox parent = ItemsControlAutomationPeer.Owner as ListBox;
            if (parent != null)
                parent.ScrollIntoView(Item);
            else
            {
                ComboBoxAutomationPeer parentPeer = ItemsControlAutomationPeer as ComboBoxAutomationPeer;
                parentPeer?.ScrollItemIntoView(Item);
            }
        }

    }
}



