// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NET46
using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace System.Drawing.Temp
{
    /// <summary>
    ///     EventArgs for the ResolveNameEventHandler.  This event is used
    ///     by the serialization process to match a name to an object
    ///     instance.
    /// </summary>
    public sealed class InstanceDescriptor
    {
        /// <summary>
        ///     Creates a new InstanceDescriptor.
        /// </summary>
        public InstanceDescriptor(MemberInfo member, ICollection arguments) : this(member, arguments, true)
        {
        }

        /// <summary>
        ///     Creates a new InstanceDescriptor.
        /// </summary>
        public InstanceDescriptor(MemberInfo member, ICollection arguments, bool isComplete)
        {
            MemberInfo = member;
            IsComplete = isComplete;

            if (arguments == null)
            {
                Arguments = Array.Empty<object>();
            }
            else
            {
                object[] args = new object[arguments.Count];
                arguments.CopyTo(args, 0);
                Arguments = args;
            }

            if (member is FieldInfo)
            {
                FieldInfo fi = (FieldInfo)member;
                if (!fi.IsStatic)
                {
                    throw new ArgumentException("InstanceDescriptorMustBeStatic");
                }
                if (Arguments.Count != 0)
                {
                    throw new ArgumentException("InstanceDescriptorLengthMismatch");
                }
            }
            else if (member is ConstructorInfo)
            {
                ConstructorInfo ci = (ConstructorInfo)member;
                if (ci.IsStatic)
                {
                    throw new ArgumentException("InstanceDescriptorCannotBeStatic");
                }
                if (Arguments.Count != ci.GetParameters().Length)
                {
                    throw new ArgumentException("InstanceDescriptorLengthMismatch");
                }
            }
            else if (member is MethodInfo)
            {
                MethodInfo mi = (MethodInfo)member;
                if (!mi.IsStatic)
                {
                    throw new ArgumentException("InstanceDescriptorMustBeStatic");
                }
                if (Arguments.Count != mi.GetParameters().Length)
                {
                    throw new ArgumentException("InstanceDescriptorLengthMismatch");
                }
            }
            else if (member is PropertyInfo)
            {
                PropertyInfo pi = (PropertyInfo)member;
                if (!pi.CanRead)
                {
                    throw new ArgumentException("InstanceDescriptorMustBeReadable");
                }
                MethodInfo mi = pi.GetGetMethod();
                if (mi != null && !mi.IsStatic)
                {
                    throw new ArgumentException("InstanceDescriptorMustBeStatic");
                }
            }
        }

        /// <summary>
        ///     The collection of arguments that should be passed to
        ///     MemberInfo in order to create an instance.
        /// </summary>
        public ICollection Arguments { get; }

        /// <summary>
        ///     Determines if the contents of this instance descriptor completely identify the instance.
        ///     This will normally be the case, but some objects may be too complex for a single method
        ///     or constructor to represent.  IsComplete can be used to identify these objects and take
        ///     additional steps to further describe their state.
        /// </summary>
        public bool IsComplete { get; }

        /// <summary>
        ///     The MemberInfo object that was passed into the constructor
        ///     of this InstanceDescriptor.
        /// </summary>
        public MemberInfo MemberInfo { get; }

        /// <summary>
        ///     Invokes this instance descriptor, returning the object
        ///     the descriptor describes.
        /// </summary>
        public object Invoke()
        {
            object[] translatedArguments = new object[Arguments.Count];
            Arguments.CopyTo(translatedArguments, 0);

            // Instance descriptors can contain other instance
            // descriptors.  Translate them if necessary.
            //
            for (int i = 0; i < translatedArguments.Length; i++)
            {
                if (translatedArguments[i] is InstanceDescriptor)
                {
                    translatedArguments[i] = ((InstanceDescriptor)translatedArguments[i]).Invoke();
                }
            }

            if (MemberInfo is ConstructorInfo)
            {
                return ((ConstructorInfo)MemberInfo).Invoke(translatedArguments);
            }
            else if (MemberInfo is MethodInfo)
            {
                return ((MethodInfo)MemberInfo).Invoke(null, translatedArguments);
            }
            else if (MemberInfo is PropertyInfo)
            {
                return ((PropertyInfo)MemberInfo).GetValue(null, translatedArguments);
            }
            else if (MemberInfo is FieldInfo)
            {
                return ((FieldInfo)MemberInfo).GetValue(null);
            }
            else
            {
                Debug.Fail($"Unrecognized reflection type in instance descriptor: {MemberInfo.GetType().Name}");
            }

            return null;
        }
    }
}
#endif