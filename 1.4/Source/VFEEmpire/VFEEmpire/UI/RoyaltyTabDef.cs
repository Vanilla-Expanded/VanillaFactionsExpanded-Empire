using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEEmpire;

public class RoyaltyTabDef : Def
{
    public bool doDividerLine = true;
    public bool hasSearch = true;
    public bool needsCharacter;

    private RoyaltyTabWorker worker;
    public Type workerClass = typeof(RoyaltyTabWorker);
    public RoyaltyTabWorker Worker => worker ??= (RoyaltyTabWorker)Activator.CreateInstance(workerClass);
}

public class RoyaltyTabWorker
{
    public virtual void DoLeftBottom(Rect inRect, MainTabWindow_Royalty parent)
    {
    }

    public virtual void DoMainSection(Rect inRect, MainTabWindow_Royalty parent)
    {
    }

    public virtual bool CheckSearch(QuickSearchFilter filter) => false;

    public virtual void Notify_Open()
    {
    }
}