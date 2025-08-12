using System.Collections.Generic;

public interface ICreatureBehaviorProvider
{
    List<BehaviorRule> GetRules(CreatureBehaviorProfileSO profile, int rangIndex);
}


