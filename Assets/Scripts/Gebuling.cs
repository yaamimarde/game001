using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Gebuling : Character
{   
    void Start()
    {
        // 游戏开始时，直接给继承过来的属性赋值
        characterName = "哥布灵";
        hp = 100;
        damage = 10;
        attack ="近战";
        defense = 10;
    }



}
