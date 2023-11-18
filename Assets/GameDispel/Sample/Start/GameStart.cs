using GameDispel;
using UnityEngine;
using static GameDispel.CommonBlockCreator;

public class GameStart : MonoBehaviour
{
    public BaseDisplayer blockDisplayer;
    public GameMap map;
    public CommonBlockCreator blockCreator;
    public Block[] Blocks;

    public int checkPoint_x;
    public int checkPoint_y;

    private void Awake()
    {

        blockCreator = new CommonBlockCreator();
        blockCreator.AddSeed(
            new BlockCreateInfo(Block.GetNormalBlock(1), 1),
            new BlockCreateInfo(Block.GetNormalBlock(2), 1),
            new BlockCreateInfo(Block.GetNormalBlock(3), 1),
            new BlockCreateInfo(Block.GetNormalBlock(4), 1),
            new BlockCreateInfo(Block.GetNormalBlock(5), 1),
            new BlockCreateInfo(Block.GetNormalBlock(6), 1));
        ConfigureGame();
    }
    [ContextMenu("配置")]
    public void ConfigureGame()
    {
        Debug.Log("开始初始化");
        
        map = new GameMap(9, 9);
        map.blockCreator = blockCreator;
        map.displayer = blockDisplayer;
        blockDisplayer.Map = map;
        map.InitMap();
    }
    [ContextMenu("检查点状态")]
    public void CheckPoint()
    {
        map.AddCheck(checkPoint_x, checkPoint_y);
        Debug.Log("检测方块id:"+map.GetBlock(checkPoint_x, checkPoint_y).id);
        if(map.CheckBlock())
        {
            Debug.Log("存在消除!");   
        }
        else
        {
            Debug.Log("无消除");
        }
    }
    [ContextMenu("手动销毁垃圾箱")]
    public void DestroyBlock()
    {
        map.DestroyBlock();
    }    
}
