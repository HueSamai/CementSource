
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using System;

namespace UnityEngine;

[RegisterTypeInIl2Cpp]
public class WaitUntil : CustomYieldInstruction
{
    public WaitUntil(IntPtr ptr) : base(ptr) { }

    private Func<bool>? m_Predicate;

    public WaitUntil(Func<bool> predicate) : base(ClassInjector.DerivedConstructorPointer<WaitUntil>())
    {
        ClassInjector.DerivedConstructorBody(this);

        m_Predicate = predicate;
    }

    public override bool keepWaiting
    {
        get
        {
            return !m_Predicate?.Invoke() ?? false;
        }
    }
}