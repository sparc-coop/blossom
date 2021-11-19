using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Sparc.Platforms.Maui
{
    public class SparcErrorBoundary : ErrorBoundary
    {
        public SparcErrorBoundary()
        {
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (CurrentException is null)
            {
                builder.AddContent(0, ChildContent);
            }
            else if (ErrorContent is not null)
            {
                builder.AddContent(1, ErrorContent(CurrentException));
            }
            else
            {
                // The default error UI doesn't include any content, because:
                // [1] We don't know whether or not you'd be happy to show the stack trace. It depends both on
                //     whether DetailedErrors is enabled and whether you're in production, because even on WebAssembly
                //     you likely don't want to put technical data like that in the UI for end users. A reasonable way
                //     to toggle this is via something like "#if DEBUG" but that can only be done in user code.
                // [2] We can't have any other human-readable content by default, because it would need to be valid
                //     for all languages.
                // Instead, the default project template provides locale-specific default content via CSS. This provides
                // a quick form of customization even without having to subclass this component.
                builder.OpenElement(2, "div");
                builder.AddAttribute(3, "class", "blazor-error-boundary");
                builder.AddContent(4, $"{CurrentException} --- {CurrentException.InnerException?.ToString()} --- {CurrentException.StackTrace}");
                builder.CloseElement();
            }
        }
    }
}
