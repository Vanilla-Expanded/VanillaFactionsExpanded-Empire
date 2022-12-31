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
    public Type workerClass = typeof(RoyaltyTabWorker);

    private RoyaltyTabWorker worker;
    public RoyaltyTabWorker Worker => worker ??= (RoyaltyTabWorker)Activator.CreateInstance(workerClass);
}

public class RoyaltyTabWorker
{
    public MainTabWindow_Royalty parent;
    public virtual void DoLeftBottom(Rect inRect) { }

    public virtual void DoMainSection(Rect inRect) { }

    public virtual bool CheckSearch(QuickSearchFilter filter) => false;

    public virtual void Notify_Open() { }
}
