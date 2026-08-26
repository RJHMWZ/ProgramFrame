using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TestMgr1 : Singleton2<TestMgr1>
{
    private static int coinSum=10;
    private static readonly Object lockCoinData=new Object(); 
    public void AddCoin(int cost)
    {
        lock (lockCoinData)
        {
            coinSum+=cost;
        }
    }

    public void SubCoin(int cost)
    {
        lock (lockCoinData)
        {
            coinSum-=cost;
        }
    } 
}
