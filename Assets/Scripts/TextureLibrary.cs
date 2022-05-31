using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Texture Library", menuName = "Scriptable Objects/Texture Library", order = 1)]
public class TextureLibrary : ScriptableObject
{
    public Sprite[][] library;

    public string[] referenceList = new string[]
    {
        "AchievementIcons",
        "Door",
        "MapTiles",
        "MenuPlus",
        "Player",
        "SavePoint",
        //"CEStudioLogo2022",
        "Tilesheet",
        "TitleFont",

        "Bullets/Boomerang",
        "Bullets/RainbowWave",

        "Entities/BreakableIcons",
        "Entities/Floatspike1",
        "Entities/Grass",
        "Entities/PixelPeople",
        "Entities/PowerGrass",
        "Entities/SnailNpc",
        "Entities/SnailNpcColor",
        "Entities/Spikey1",
        "Entities/TurtleNpc",

        "Items/Boomerang",
        "Items/HeartContainer",
        "Items/HelixFragment",
        "Items/RainbowWave",

        "Particles/Bubble",
        "Particles/Explosion",
        "Particles/Nom",
        "Particles/Splash",
        "Particles/Star",

        "UI/AchievementPanel",
        "UI/DebugIcons",
        "UI/DebugKey",
        "UI/DialogueBox",
        "UI/DialogueIcon2",
        "UI/DialoguePortrait",
        "UI/FontSprites",
        "UI/GenericHighlightBox",
        "UI/Heart",
        "UI/LoadingIcon",
        "UI/Minimap",
        "UI/MinimapIcons",
        "UI/MinimapMask",
        "UI/MinimapPanel",
        "UI/WeaponIcons"
    };

    public Sprite[] Unpack(Sprite texture, int sliceWidth, int sliceHeight, string name)
    {
        Texture2D newTexture = new Texture2D((int)texture.rect.width, (int)texture.rect.height);
        newTexture.SetPixels(texture.texture.GetPixels((int)texture.textureRect.x, (int)texture.textureRect.y,
            (int)texture.textureRect.width, (int)texture.textureRect.height));
        newTexture.Apply();
        return Unpack(newTexture, sliceWidth, sliceHeight, name);
    }

    public Sprite[] Unpack(Texture2D texture, int sliceWidth, int sliceHeight, string name)
    {
        List<Sprite> unpackedArray = new List<Sprite>();
        int counter = 0;
        for (int i = texture.height - sliceHeight; i >= 0; i -= sliceHeight)
        {
            for (int j = 0; j < texture.width; j += sliceWidth)
            {
                Sprite newSprite = Sprite.Create(texture, new Rect(j, i, sliceWidth, sliceHeight), new Vector2(0.5f, 0.5f), 16);
                newSprite.name = name + " " + counter;
                unpackedArray.Add(newSprite);
                counter++;
            }
        }
        Sprite[] finalArray = unpackedArray.ToArray();
        return finalArray;
    }

    public Vector2 GetSpriteSize(string name)
    {
        Vector2 size = Vector2.zero;
        int i = 0;
        while (i < PlayState.spriteSizeLibrary.Length && size == Vector2.zero)
        {
            if (PlayState.spriteSizeLibrary[i].name == name)
                size = new Vector2(PlayState.spriteSizeLibrary[i].width, PlayState.spriteSizeLibrary[i].height);
            i++;
        }
        return size;
    }

    public void BuildDefaultLibrary()
    {
        List<Sprite[]> newLibrary = new List<Sprite[]>();
        for (int i = 0; i < referenceList.Length; i++)
        {
            Vector2 thisSize = GetSpriteSize(referenceList[i]);
            if (thisSize == Vector2.zero || thisSize.x < 0 || thisSize.y < 0)
                thisSize = new Vector2(16, 16);
            Debug.Log(referenceList[i]);
            newLibrary.Add(Unpack((Texture2D)Resources.Load("Images/" + referenceList[i]), (int)thisSize.x, (int)thisSize.y, referenceList[i]));
        }
        library = newLibrary.ToArray();
        GetNewTextWidths();
    }

    public void BuildDefaultAnimLibrary()
    {
        TextAsset animJson = Resources.Load<TextAsset>("Animations");
        PlayState.AnimationLibrary newLibrary = JsonUtility.FromJson<PlayState.AnimationLibrary>(animJson.text);
        PlayState.animationLibrary = newLibrary.animArray;
    }

    public void BuildDefaultSpriteSizeLibrary()
    {
        TextAsset sizeJson = Resources.Load<TextAsset>("SpriteSizes");
        PlayState.SpriteSizeLibrary newLibrary = JsonUtility.FromJson<PlayState.SpriteSizeLibrary>(sizeJson.text);
        PlayState.spriteSizeLibrary = newLibrary.sizeArray;
    }

    public void BuildDefaultTilemap()
    {
        foreach (Transform layer in GameObject.Find("Grid").transform)
        {
            if (layer.name != "Special")
            {
                Tilemap map = layer.GetComponent<Tilemap>();
                List<int> swappedIDs = new List<int>();
                for (int y = 0; y < map.size.y; y++)
                {
                    for (int x = 0; x < map.size.x; x++)
                    {
                        Vector3Int worldPos = new Vector3Int(Mathf.RoundToInt(map.origin.x - (map.size.x * 0.5f) + x), Mathf.RoundToInt(map.origin.y - (map.size.y * 0.5f) + y), 0);
                        if (map.GetSprite(worldPos) != null)
                        {
                            Sprite tileSprite = map.GetSprite(worldPos);
                            Debug.Log(tileSprite.name);
                            int spriteID = int.Parse(tileSprite.name.Split('_')[1]);
                            if (!swappedIDs.Contains(spriteID))
                            {
                                TileBase tile = map.GetTile(worldPos);
                                Tile newTile = CreateInstance<Tile>();
                                Debug.Log(PlayState.GetSprite("Tilesheet", spriteID).name);
                                newTile.sprite = PlayState.GetSprite("Tilesheet", spriteID);
                                map.SwapTile(tile, newTile);
                                swappedIDs.Add(spriteID);
                            }
                        }
                    }
                }
            }
        }
    }

    public void BuildLibrary(string folderPath = null)
    {
        BuildDefaultLibrary();
        if (folderPath != null)
        {

        }
        GetNewTextWidths();
    }

    public void BuildAnimationLibrary(string dataPath = null)
    {
        BuildDefaultAnimLibrary();
        if (dataPath != null)
        {
            PlayState.LoadNewAnimationLibrary(dataPath);
        }
    }

    public void BuildSpriteSizeLibrary(string dataPath = null)
    {
        BuildDefaultSpriteSizeLibrary();
        if (dataPath != null)
        {
            PlayState.LoadNewSpriteSizeLibrary(dataPath);
        }
    }

    public void GetNewTextWidths()
    {
        List<int> newWidths = new List<int>();
        for (int i = 0; i < 94; i++)
        {
            Sprite letter = PlayState.GetSprite("UI/FontSprites", i);
            int totalWidth = 0;
            int emptySpaceFound = 0;
            for (int x = 0; x < letter.rect.width; x++)
            {
                bool found = false;
                for (int y = 0; y < letter.rect.height; y++)
                {
                    if (letter.texture.GetPixel(x, y) != new Color32(0, 0, 0, 0))
                        found = true;
                }
                if (!found && totalWidth != 0)
                    emptySpaceFound++;
                else if (found)
                {
                    totalWidth++;
                    while (emptySpaceFound > 0)
                    {
                        totalWidth++;
                        emptySpaceFound--;
                    }
                }
            }
            newWidths.Add(totalWidth);
        }
        newWidths.Add(10);
        PlayState.charWidths = newWidths.ToArray();
    }
}
