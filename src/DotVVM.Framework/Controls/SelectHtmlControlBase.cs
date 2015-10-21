using DotVVM.Framework.Runtime;
using DotVVM.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotVVM.Framework.Binding;

namespace DotVVM.Framework.Controls
{
	/// <summary>
	/// Renders <c>select</c> HTML element control.
	/// </summary>
	public abstract class SelectHtmlControlBase : Selector
	{
        /// <summary>
        /// Gets or sets a value indicating whether the control is enabled and can be modified.
        /// </summary>
        public bool Enabled
        {
            get { return (bool)GetValue(EnabledProperty); }
            set { SetValue(EnabledProperty, value); }
        }
        public static readonly DotvvmProperty EnabledProperty =
            DotvvmProperty.Register<bool, SelectHtmlControlBase>(t => t.Enabled, true);

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectHtmlControlBase"/> class.
        /// </summary>
        public SelectHtmlControlBase()
			: base("select")
        {
            
        }

        /// <summary>
        /// Adds all attributes that should be added to the control begin tag.
        /// </summary>
        protected override void AddAttributesToRender(IHtmlWriter writer, RenderContext context)
        {

            writer.AddKnockoutDataBind("enable", this, EnabledProperty, () =>
            {
                if (!Enabled)
                {
                    writer.AddAttribute("disabled", "disabled");
                }
            });

            if (!RenderOnServer)
            {
                writer.AddKnockoutDataBind("options", this, DataSourceProperty, () => { });

                if (!string.IsNullOrEmpty(DisplayMember))
                {
                    writer.AddKnockoutDataBind("optionsText", KnockoutHelper.MakeStringLiteral(DisplayMember));
                }
                if (!string.IsNullOrEmpty(ValueMember))
                {
                    writer.AddKnockoutDataBind("optionsValue", KnockoutHelper.MakeStringLiteral(ValueMember));
                }
            }

			// changed event
			var selectionChangedBinding = GetCommandBinding(SelectionChangedProperty);
			if (selectionChangedBinding != null)
			{
				writer.AddAttribute("onchange", KnockoutHelper.GenerateClientPostBackScript(selectionChangedBinding, context, this, isOnChange: true,useWindowSetTimeout:true));
			}

			// selected value
            writer.AddKnockoutDataBind("value", this, SelectedValueProperty, () => { });
            
            base.AddAttributesToRender(writer, context);
        }

        /// <summary>
        /// Renders the contents inside the control begin and end tags.
        /// </summary>
        protected override void RenderContents(IHtmlWriter writer, RenderContext context)
        {
            base.RenderContents(writer, context);
            if (RenderOnServer)
            {
                // render items
                bool first = true;
                foreach (var item in GetIEnumerableFromDataSource(DataSource))
                {
                    var value = string.IsNullOrEmpty(ValueMember) ? item : ReflectionUtils.GetObjectProperty(item, ValueMember);
                    var text = string.IsNullOrEmpty(DisplayMember) ? item : ReflectionUtils.GetObjectProperty(item, DisplayMember);

                    if (first)
                    {
                        writer.WriteUnencodedText(Environment.NewLine);
                        first = false;
                    }
                    writer.WriteUnencodedText("    ");  //Indent
                    writer.AddAttribute("value", value != null ? value.ToString() : "");
                    writer.RenderBeginTag("option");
                    writer.WriteText(text != null ? text.ToString() : "");
                    writer.RenderEndTag();
                    writer.WriteUnencodedText(Environment.NewLine);
                }
            }
        }
	}
}
