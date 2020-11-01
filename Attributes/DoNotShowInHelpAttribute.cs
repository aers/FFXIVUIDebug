using System;

namespace FFXIVUIDebug
{
    [AttributeUsage(AttributeTargets.Method)]
    public class DoNotShowInHelpAttribute : Attribute
    {
    }
}