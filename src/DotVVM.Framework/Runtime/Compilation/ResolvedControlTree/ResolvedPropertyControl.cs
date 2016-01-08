﻿using DotVVM.Framework.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotVVM.Framework.Runtime.Compilation.AbstractControlTree;

namespace DotVVM.Framework.Runtime.Compilation.ResolvedControlTree
{
    public class ResolvedPropertyControl : ResolvedPropertySetter, IAbstractPropertyControl
    {
        public ResolvedControl Control { get; set; }

        IAbstractControl IAbstractPropertyControl.Control => Control;

        public ResolvedPropertyControl(DotvvmProperty property, ResolvedControl control)
            : base(property)
        {
            Control = control;
        }

        public override void Accept(IResolvedControlTreeVisitor visitor)
        {
            visitor.VisitPropertyControl(this);
        }

        public override void AcceptChildren(IResolvedControlTreeVisitor visitor)
        {
            Control?.Accept(visitor);
        }
    }
}
