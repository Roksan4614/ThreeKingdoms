using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class StoryMode_Node_30a : StoryModeBaseComponent
{
    protected async override UniTask StartAsync()
    {
        await UniTask.Yield();
    }

}
