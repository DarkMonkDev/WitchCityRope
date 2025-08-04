using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace WitchCityRope.Web.Tests.Helpers
{
    /// <summary>
    /// Extension methods to simplify bunit test code
    /// </summary>
    public static class BunitExtensions
    {
        /// <summary>
        /// Clicks an element without needing to specify MouseEventArgs
        /// </summary>
        public static void Click(this IElement element)
        {
            element.Click(new MouseEventArgs());
        }

        /// <summary>
        /// Clicks an element asynchronously without needing to specify MouseEventArgs
        /// </summary>
        public static Task ClickAsync(this IElement element)
        {
            return element.ClickAsync(new MouseEventArgs());
        }



        /// <summary>
        /// Gets the CSS classes of an element as a list
        /// </summary>
        public static IEnumerable<string> GetClasses(this IElement element)
        {
            var classAttribute = element.GetAttribute("class");
            if (string.IsNullOrWhiteSpace(classAttribute))
                return Enumerable.Empty<string>();
            
            return classAttribute.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Finds a descendant element matching the specified CSS selector
        /// </summary>
        public static IElement Find(this IElement element, string cssSelector)
        {
            return element.QuerySelector(cssSelector);
        }

        /// <summary>
        /// Finds all descendant elements matching the specified CSS selector
        /// </summary>
        public static IEnumerable<IElement> FindAll(this IElement element, string cssSelector)
        {
            return element.QuerySelectorAll(cssSelector);
        }

        /// <summary>
        /// Gets all attributes of an element as a dictionary
        /// </summary>
        public static IDictionary<string, string> GetAttributes(this IElement element)
        {
            var attributes = new Dictionary<string, string>();
            foreach (var attr in element.Attributes)
            {
                attributes[attr.Name] = attr.Value;
            }
            return attributes;
        }

    }
}