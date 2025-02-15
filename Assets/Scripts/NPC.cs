﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC:MonoBehaviour, IRoomObject, ICutsceneObject {
    #region vars
    public int ID = 0;
    public int lookMode = 0;
    public bool upsideDown = false;
    public string nameID = "pleaseNameMe";
    public int animationSet = 0;

    public bool chatting = false;
    public bool needsSpace = false; // On the off chance that two snails are close enough to each other to trigger simultaneously, like 06 and 17
    public bool hasLongDialogue = false;
    public bool buttonDown = false;
    public List<Color32> colors = new();
    public List<int> portraitStateList = new();         // 0 for the player, any other positive number for whatever other NPC is speaking
    public Texture2D colorTable;
    public Sprite[] npcSpriteSheet;
    public Sprite[] sprites;
    public SpriteRenderer sprite;
    public AnimationModule anim;
    public GameObject speechBubble;
    public SpriteRenderer speechBubbleSprite;
    public AnimationModule speechBubbleAnim;
    public TextObject speechBubbleControl;
    public bool bubbleState = false;
    public Vector2 origin;
    public float velocity;
    public List<string> textToSend = new();

    private int nexted = 0;
    private RaycastHit2D groundCheck;
    private const float GRAVITY = 1.25f;
    private const float TERMINAL_VELOCITY = -0.5208f;
    private float floatTheta = 0;
    private int bubbleControlAppearFrame;
    #endregion vars

    #region cutscene
    public void cutRegister() {

    }

    public void cutStart() {

    }

    public void cutEnd() {

    }
    #endregion cutscene

    public Dictionary<string, object> resave()
    {
        return null;
    }

    public string myType = "NPC";

    public string objType
    {
        get
        {
            return myType;
        }
    }

    public Dictionary<string, object> save()
    {
        Dictionary<string, object> content = new();
        content["ID"] = ID;
        content["lookMode"] = lookMode;
        content["upsideDown"] = upsideDown;
        content["nameID"] = nameID;
        content["animationSet"] = animationSet;
        return content;
    }

    public void load(Dictionary<string, object> content)
    {
        ID = (int)content["ID"];
        lookMode = (int)content["lookMode"];
        upsideDown = (bool)content["upsideDown"];
        nameID = (string)content["nameID"];
        animationSet = (int)content["animationSet"];
        Spawn();
    }

    public virtual void Awake()
    {
        if (PlayState.gameState != PlayState.GameState.game)
            return;

        speechBubble = transform.Find("Speech Bubble").gameObject;
        speechBubbleSprite = speechBubble.GetComponent<SpriteRenderer>();
        speechBubbleAnim = speechBubble.GetComponent<AnimationModule>();
        speechBubbleControl = speechBubble.transform.Find("Control Icon").GetComponent<TextObject>();
        bubbleControlAppearFrame = PlayState.GetAnim("NPC_bubble_data").frames[0];

        sprite = GetComponent<SpriteRenderer>();
        anim = GetComponent<AnimationModule>();
        anim.updateSprite = false;

        nexted = 0;
        chatting = false;
        speechBubbleSprite.enabled = false;
        floatTheta = UnityEngine.Random.Range(0, PlayState.TAU);

        origin = transform.localPosition;

        groundCheck = Physics2D.BoxCast(
            transform.position,
            new Vector2(1.467508f, 0.82375f),
            0,
            upsideDown ? Vector2.up : Vector2.down,
            Mathf.Infinity,
            LayerMask.GetMask("PlayerCollide"),
            Mathf.Infinity,
            Mathf.Infinity
            );

        PlayState.globalFunctions.CreateLightMask(12, transform);
    }

    public virtual void Spawn()
    {
        List<int> IDsDeletedByCurrentChar = PlayState.currentProfile.character switch
        {
            "Upside" => new() { 17 },
            "Leggy" => new() { 56 },
            _ => new() { }
        };
        if (IDsDeletedByCurrentChar.Contains(ID))
        {
            Destroy(gameObject);
            return;
        }

        CreateNewSprites();
        anim.Add("NPC_" + animationSet + "_idle");
        anim.Add("NPC_" + animationSet + "_shell");
        anim.Add("NPC_" + animationSet + "_sleep");
        if ((ID == 26 && (PlayState.currentProfile.character == "Sluggy" || PlayState.currentProfile.character == "Leechy")) ||
            (ID == 38 && PlayState.CountFragments() < PlayState.MAX_FRAGMENTS))
        {
            anim.Play("NPC_" + animationSet + "_sleep");
            lookMode = 2;
            if (ID == 38)
                transform.position += new Vector3(1, -2, 0);
        }
        else
            anim.Play("NPC_" + animationSet + "_idle");
        if (upsideDown)
        {
            sprite.flipY = true;
            //speechBubbleSprite.flipY = true;
            //speechBubble.transform.localPosition = new Vector2(0, -0.75f);
            speechBubble.transform.localPosition = new Vector2(0, -speechBubble.transform.localPosition.y);
        }
        speechBubbleAnim.Add(string.Format("NPC_bubble_open_{0}", upsideDown ? "up" : "down"));
        speechBubbleAnim.Add(string.Format("NPC_bubble_close_{0}", upsideDown ? "up" : "down"));
        speechBubbleControl.gameObject.SetActive(false);
        if (ID == 38)
        {
            if (PlayState.IsBossAlive(3))
                Destroy(gameObject);
            else if (PlayState.CountFragments() == PlayState.MAX_FRAGMENTS && PlayState.GetNPCVar(PlayState.NPCVarIDs.SeenSunEnding) == 0)
            {
                PlayState.SetNPCVar(PlayState.NPCVarIDs.SeenSunEnding, 1);
                PlayState.credits.StartCredits(PlayState.currentProfile.gameTime);
            }
        }
    }

    public virtual void FixedUpdate()
    {
        if (PlayState.gameState != PlayState.GameState.game)
            return;

        if ((ID == 38 && anim.currentAnimName != "NPC_0_sleep") || ID == 39)
        {
            floatTheta += Time.fixedDeltaTime;
            transform.localPosition = new Vector2(origin.x, origin.y + Mathf.Sin(floatTheta * 0.5f) * 0.3125f);
            return;
        }

        groundCheck = Physics2D.BoxCast(
            transform.position,
            new Vector2(1, 0.98f),
            0,
            velocity > 0 ? Vector2.up : Vector2.down,
            Mathf.Infinity,
            LayerMask.GetMask("PlayerCollide"),
            Mathf.Infinity,
            Mathf.Infinity
            );
        if (groundCheck.distance != 0 && groundCheck.distance > 0.01f) {
            if (upsideDown) {
                velocity = Mathf.Clamp(velocity + GRAVITY * Time.fixedDeltaTime, -Mathf.Infinity, -TERMINAL_VELOCITY);
            } else {
                velocity = Mathf.Clamp(velocity - GRAVITY * Time.fixedDeltaTime, TERMINAL_VELOCITY, Mathf.Infinity);
            }
            bool resetVelFlag = false;
            if (Mathf.Abs(velocity) > Mathf.Abs(groundCheck.distance)) {
                RaycastHit2D groundCheckRay = Physics2D.Raycast(
                    new Vector2(groundCheck.point.x, transform.position.y + (upsideDown ? 0.5f : -0.5f)),
                    velocity > 0 ? Vector2.up : Vector2.down,
                    Mathf.Infinity,
                    LayerMask.GetMask("PlayerCollide"),
                    Mathf.Infinity,
                    Mathf.Infinity
                    );
                velocity = groundCheckRay.distance * (upsideDown ? 1 : -1);
                resetVelFlag = true;
            }
            transform.position = new Vector2(transform.position.x, transform.position.y + velocity);
            if (resetVelFlag)
                velocity = 0;
        } else {
            velocity = 0;
        }
    }

    public virtual void Update()
    {
        if (PlayState.gameState == PlayState.GameState.game)
        {
            if (anim.isPlaying)
                sprite.sprite = sprites[anim.GetCurrentFrameValue()];

            if (!PlayState.cutsceneActive)
            {
                //if (lookMode == 0)
                //{
                //    if (PlayState.player.transform.position.x < transform.position.x && anim.currentAnimName != "NPC_sleep")
                //    {
                //        sprite.flipX = true;
                //        speechBubbleSprite.flipX = false;
                //    }
                //    else
                //    {
                //        sprite.flipX = false;
                //        speechBubbleSprite.flipX = true;
                //    }
                //}
                //else if (lookMode == 1)
                //{
                //    sprite.flipX = true;
                //    speechBubbleSprite.flipX = false;
                //}
                //else
                //{
                //    sprite.flipX = false;
                //    speechBubbleSprite.flipX = true;
                //}
                if (lookMode == 0)
                    sprite.flipX = PlayState.player.transform.position.x < transform.position.x && anim.currentAnimName != "NPC_sleep";
                else
                    sprite.flipX = lookMode == 2;
            }

            if (Vector2.Distance(transform.position, PlayState.player.transform.position) < 1.5f && !chatting && !needsSpace)
            {
                if (!PlayState.isTalking)
                {
                    textToSend.Clear();
                    portraitStateList.Clear();
                    bool intentionallyEmpty = false;

                    // Box shape
                    int boxShape = ID switch
                    {
                        38 or 39 => 4,
                        17 => PlayState.GetAnim("Dialogue_characterShapes").frames[2],
                        56 => PlayState.GetAnim("Dialogue_characterShapes").frames[3],
                        _ => 0
                    };

                    // Box color
                    string boxColor = ID switch
                    {
                        29 or 30 or 31 or 32 or 33 or 34 or 35 or 36 or 37 or 45 or 47 => "0002",
                        38 => "0009",
                        39 => "0102",
                        17 => PlayState.ParseColorCodeToString(PlayState.GetAnim("Dialogue_characterColors").frames[2]),
                        56 => PlayState.ParseColorCodeToString(PlayState.GetAnim("Dialogue_characterColors").frames[3]),
                        _ => "0005"
                    };

                    bool useRandoDialogue = false;
                    if (PlayState.isRandomGame && PlayState.currentRando.npcTextShuffled)
                    {
                        int hintID = -1;
                        for (int i = 0; i < PlayState.currentRando.npcHintData.Length; i += 4)
                        {
                            if (PlayState.currentRando.npcHintData[i] == ID)
                            {
                                hintID = i;
                                i = PlayState.currentRando.npcHintData.Length;
                            }
                        }
                        if (hintID != -1)
                        {
                            AddText(string.Concat("HINT|", hintID));
                            useRandoDialogue = true;
                        }
                        else if (ID < PlayState.currentRando.npcTextIndeces.Length)
                        {
                            AddText("FLAVOR");
                            useRandoDialogue = true;
                        }
                    }
                    if (!useRandoDialogue)
                    {
                        switch (ID)
                        {
                            case 0:
                                if (!PlayState.CheckForItem(PlayState.Items.Peashooter) &&
                                    !PlayState.CheckForItem(PlayState.Items.Boomerang) && !PlayState.CheckForItem(PlayState.Items.SSBoom))
                                {
                                    if (PlayState.currentProfile.character == "Leggy")
                                        AddText("explainLeggyFlip");
                                    else
                                        AddText("explainWallClimb");
                                }
                                else if (!PlayState.CheckForItem(PlayState.Items.Boomerang) && !PlayState.CheckForItem(PlayState.Items.SSBoom))
                                    AddText("explainPeashooter");
                                else if (!PlayState.CheckForItem(PlayState.Items.RainbowWave) && !PlayState.CheckForItem(PlayState.Items.DebugRW))
                                    AddText("explainBoomerang");
                                else if (!PlayState.CheckForItem(PlayState.Items.Devastator))
                                    AddText("explainRainbowWave");
                                else if (PlayState.GetItemPercentage() < 100)
                                    AddText("explainDevastator");
                                else
                                    AddText("default");
                                break;

                            case 1:
                                nexted = 1;
                                if (!PlayState.hasJumped && !PlayState.CheckForItem(PlayState.Items.Peashooter) &&
                                    !PlayState.CheckForItem(PlayState.Items.Boomerang) && !PlayState.CheckForItem(PlayState.Items.SSBoom))
                                {
                                    nexted = 0;
                                    AddText("promptJump");
                                }
                                else if (!PlayState.CheckForItem(PlayState.Items.Peashooter))
                                    AddText("promptStory");
                                else if (PlayState.GetItemPercentage() < 100)
                                    AddText("smallTalk");
                                else
                                    AddText("default");
                                break;

                            case 2:
                                if (PlayState.isRandomGame)
                                    AddText("rando");
                                else if (!PlayState.CheckForItem(PlayState.Items.Peashooter) &&
                                    !PlayState.CheckForItem(PlayState.Items.Boomerang) && !PlayState.CheckForItem(PlayState.Items.SSBoom))
                                    AddText("predictPeashooter");
                                else if (PlayState.IsBossAlive(0) && PlayState.IsBossAlive(1))
                                    AddText("predictShellbreaker");
                                else if (!PlayState.CheckForItem(PlayState.Items.Boomerang) && !PlayState.CheckForItem(PlayState.Items.SSBoom))
                                    AddText("predictBoomerang");
                                else if (PlayState.IsBossAlive(1))
                                    AddText("predictStompy");
                                else if (!PlayState.CheckForItem(PlayState.Items.RainbowWave))
                                    AddText("predictRainbowWave");
                                else if (PlayState.IsBossAlive(2))
                                    AddText("predictSpaceBox");
                                else if (!PlayState.CheckForItem(PlayState.Items.RapidFire))
                                    AddText("predictRapidFire");
                                else if (PlayState.IsBossAlive(3))
                                    AddText("predictMoonSnail");
                                else if (PlayState.CountFragments() < 30)
                                    AddText("predictHelixFragments");
                                else if (PlayState.GetNPCVar(PlayState.NPCVarIDs.HasSeenIris) != 1)
                                    AddText("predictIris");
                                else
                                    AddText("default");
                                break;

                            case 3:
                                if (!PlayState.CheckForItem(PlayState.Items.FlyShell))
                                    AddText("cantCorner");
                                else if (!PlayState.CheckForItem(PlayState.Items.MetalShell))
                                    AddText("admireGravity");
                                else
                                    AddText("default");
                                break;

                            case 4:
                                if (PlayState.IsTileSolid(new Vector2(transform.position.x - 2.5f, transform.position.y)))
                                    AddText("greenBlock");
                                else
                                    AddText("default");
                                break;

                            case 5:
                                if (PlayState.GetItemPercentage() < 100)
                                    AddText("secrets");
                                else
                                    AddText("default");
                                break;

                            case 6:
                                if (PlayState.CheckForItem(PlayState.Items.HighJump) || PlayState.CheckForItem(PlayState.Items.FlyShell) ||
                                    PlayState.currentProfile.character == "Upside")
                                    AddText("treehouses");
                                else
                                    AddText("default");
                                break;

                            case 7:
                                if (!PlayState.CheckForItem(PlayState.Items.Peashooter))
                                    AddText("save");
                                else if (!PlayState.CheckForItem(PlayState.Items.Boomerang))
                                    AddText("peashooter");
                                else if (!PlayState.CheckForItem(PlayState.Items.RainbowWave))
                                    AddText("boomerang");
                                else if (!PlayState.CheckForItem(PlayState.Items.Devastator))
                                    AddText("rainbowWave");
                                else if (PlayState.IsBossAlive(3))
                                    AddText("scared");
                                else
                                    AddText("default");
                                break;

                            case 8:
                                if (PlayState.IsTileSolid(new Vector2(transform.position.x + 8.5f, transform.position.y)))
                                    AddText("suspicious");
                                else
                                    AddText("default");
                                break;

                            case 9:
                                if (PlayState.currentProfile.percentage < 100)
                                    AddText("dirtHome");
                                else
                                    AddText("default");
                                break;

                            case 10:
                                AddText("default");
                                break;

                            case 11:
                                if (PlayState.isRandomGame)
                                    AddText("rando");
                                else if (!PlayState.isRandomGame && !PlayState.CheckForItem(PlayState.Items.Peashooter))
                                    AddText("explainPeashooter");
                                else
                                    AddText("default");
                                break;

                            case 12:
                                if (CountItemsInRoom() > 0 && !PlayState.isRandomGame)
                                    AddText("funBlocks");
                                else
                                    AddText("default");
                                break;

                            case 13:
                                AddText("default");
                                break;

                            case 14:
                                if (PlayState.CountFragments() < 15)
                                    AddText("helixFragments");
                                else if (PlayState.CountFragments() < 30 || PlayState.GetNPCVar(PlayState.NPCVarIDs.HasSeenIris) != 1)
                                    AddText("shrine");
                                else
                                    AddText("default");
                                break;

                            case 15:
                                if (PlayState.GetItemPercentage() < 20)
                                    AddText("hintSecret");
                                else if (PlayState.GetItemPercentage() < 40)
                                {
                                    switch (PlayState.currentProfile.character)
                                    {
                                        case "Snaily":
                                            AddText("hintSnaily");
                                            break;
                                        case "Sluggy":
                                            AddText("hintSluggy");
                                            break;
                                        case "Upside":
                                            AddText("hintUpside");
                                            break;
                                        case "Leggy":
                                            AddText("hintLeggy");
                                            break;
                                        case "Blobby":
                                            AddText("hintBlobby");
                                            break;
                                        case "Leechy":
                                            AddText("hintLeechy");
                                            break;
                                    }
                                }
                                else if (PlayState.GetItemPercentage() < 60)
                                    AddText("hintMissedSecret");
                                else if (PlayState.GetItemPercentage() < 80)
                                    AddText("hintEarlyHighJump");
                                else if (PlayState.GetItemPercentage() < 100)
                                    AddText("hintSSB");
                                else
                                    AddText("default");
                                break;

                            case 16:
                                if (!PlayState.CheckForItem(PlayState.Items.Peashooter) &&
                                    !PlayState.CheckForItem(PlayState.Items.Boomerang) && !PlayState.CheckForItem(PlayState.Items.SSBoom))
                                {
                                    if (PlayState.currentProfile.character == "Leechy")
                                        AddText("healTipLeechy");
                                    else
                                        AddText("healTipGeneric");
                                }
                                else if (transform.localPosition.y > origin.y - 21)
                                    AddText("ride");
                                else
                                    AddText("default");
                                break;

                            case 17:
                                if (PlayState.GetItemPercentage() < 100)
                                    AddText("secret");
                                else
                                    AddText("default");
                                break;

                            case 18:
                                if (PlayState.isRandomGame)
                                    AddText("rando");
                                else if (PlayState.CheckForItem(PlayState.Items.SSBoom))
                                    AddText("remindShoot");
                                else
                                    AddText("default");
                                break;

                            case 19:
                                if (!PlayState.CheckForItem(PlayState.Items.Boomerang) && !PlayState.CheckForItem(PlayState.Items.SSBoom))
                                    AddText("boomerang");
                                else
                                    AddText("default");
                                break;

                            case 20:
                                if (!PlayState.CheckForItem(PlayState.Items.Boomerang) && !PlayState.CheckForItem(PlayState.Items.SSBoom))
                                    AddText("secret");
                                else if (PlayState.GetItemPercentage() < 100)
                                    AddText("findSnails");
                                else
                                    AddText("default");
                                break;

                            case 21:
                                if (PlayState.isRandomGame)
                                    AddText("rando");
                                else if (!PlayState.CheckForItem(PlayState.Items.Boomerang))
                                    AddText("boomerang");
                                else
                                    AddText("default");
                                break;

                            case 22:
                                if (PlayState.GetItemPercentage() < 100)
                                    AddText("thorgleBorgle");
                                else
                                    AddText("default");
                                PlayState.SetNPCVar(PlayState.NPCVarIDs.TalkedToCaveSnail, 1);
                                break;

                            case 23:
                                if (PlayState.GetItemPercentage() < 100 && PlayState.GetNPCVar(PlayState.NPCVarIDs.TalkedToCaveSnail) != 1)
                                    AddText("caveSnail");
                                else if (PlayState.GetItemPercentage() < 60)
                                    AddText("loadGame");
                                else
                                    AddText("default");
                                break;

                            case 24:
                                if (PlayState.isRandomGame)
                                    AddText("rando");
                                else if (CountItemsInRoom() > 0)
                                    AddText("offerHelixFragment");
                                else
                                    AddText("default");
                                break;

                            case 25:
                                if (!PlayState.CheckForItem(PlayState.Items.Peashooter))
                                    AddText("explainMap");
                                else
                                    AddText("default");
                                break;

                            case 26:
                                if (PlayState.isRandomGame && CountItemsInRoom() > 0)
                                    AddText("offerRando");
                                else if (PlayState.isRandomGame)
                                    AddText("emptyRando");
                                else if (PlayState.currentProfile.character == "Blobby")
                                    AddText("blobby");
                                else if (PlayState.currentProfile.character == "Snaily" ||
                                    PlayState.currentProfile.character == "Upside" || PlayState.currentProfile.character == "Leggy")
                                    AddText("default");
                                else
                                    intentionallyEmpty = true;
                                break;

                            case 27:
                                if (PlayState.CheckForItem(PlayState.Items.FlyShell) || PlayState.CheckForItem(PlayState.Items.MetalShell))
                                    AddText("powerfulPlayer");
                                else if (PlayState.CheckForItem(PlayState.Items.IceShell))
                                    AddText("icyPlayer");
                                else
                                    AddText("default");
                                break;

                            case 28:
                                if (PlayState.CheckForItem(PlayState.Items.FlyShell))
                                {
                                    AddText(PlayState.currentProfile.character switch
                                    {
                                        "Upside" => "magneticFoot",
                                        "Leggy" => "corkscrewJump",
                                        "Blobby" => "angelJump",
                                        _ => "gravSnail" + PlayState.generalData.gravSwapType.ToString()
                                    });
                                }
                                else
                                    AddText("default");
                                break;

                            case 29:
                                if (PlayState.currentProfile.difficulty == 2)
                                    AddText("insane");
                                else
                                    AddText("default");
                                break;

                            case 30:
                                if (!PlayState.CheckForItem(PlayState.Items.MetalShell) || !PlayState.CheckForItem(PlayState.Items.RapidFire))
                                    AddText("underpowered");
                                else if (PlayState.IsBossAlive(3))
                                    AddText("warnAboutMoonSnail");
                                else if (PlayState.CountFragments() < 30 || PlayState.GetNPCVar(PlayState.NPCVarIDs.HasSeenIris) != 1)
                                    AddText("helixFragments");
                                else
                                    AddText("default");
                                break;

                            case 31:
                                if (PlayState.IsBossAlive(3))
                                    AddText("discussMoonSnail");
                                else if (PlayState.CountFragments() < 30 && (!PlayState.IsBossAlive(3) || PlayState.GetNPCVar(PlayState.NPCVarIDs.HasSeenIris) != 1))
                                    AddText("helixFragments");
                                else
                                    AddText("default");
                                break;

                            case 32:
                                if (!PlayState.CheckForItem(PlayState.Items.MetalShell) || !PlayState.CheckForItem(PlayState.Items.RapidFire))
                                    AddText("underpowered");
                                else if (PlayState.CountFragments() < 30 && PlayState.IsBossAlive(3))
                                    AddText("noIris");
                                else if (PlayState.CountFragments() == 30 && PlayState.IsBossAlive(3))
                                    AddText("poweredIris");
                                else if (PlayState.CountFragments() < 30 && PlayState.GetNPCVar(PlayState.NPCVarIDs.HasSeenIris) != 1)
                                    AddText("helixFragments");
                                else
                                    AddText("default");
                                break;

                            case 33:
                                if (!PlayState.IsBossAlive(3))
                                    AddText("celebrate");
                                else if (PlayState.isRandomGame && CountItemsInRoom() > 0)
                                    AddText("offerRando");
                                else if (CountItemsInRoom() > 0)
                                    AddText("offerHeart");
                                else
                                    AddText("default");
                                break;

                            case 34:
                                if (PlayState.CountFragments() < 30 || PlayState.GetNPCVar(PlayState.NPCVarIDs.HasSeenIris) == 0)
                                    AddText("findFragments");
                                else
                                    AddText("default");
                                break;

                            case 35:
                                AddText("default");
                                break;

                            case 36:
                                if (PlayState.isRandomGame)
                                    AddText("rando");
                                else
                                    AddText("default");
                                break;

                            case 37:
                                AddText("default");
                                break;

                            case 38:
                                if (PlayState.CountFragments() == PlayState.MAX_FRAGMENTS)
                                    AddText("thank");
                                else
                                    AddText("default");
                                break;

                            case 39:
                                PlayState.SetNPCVar(PlayState.NPCVarIDs.HasSeenIris, 1);
                                int helixes = PlayState.CountFragments();
                                int helixesLeft = PlayState.MAX_FRAGMENTS - PlayState.CountFragments();
                                if (PlayState.IsBossAlive(3))
                                {
                                    if (helixes == 0)
                                        AddText("noFragments");
                                    else if (helixes == 1)
                                        AddText("oneFragment");
                                    else if (helixesLeft > 5)
                                        AddText("someFragments");
                                    else if (helixesLeft > 1)
                                        AddText("mostFragments");
                                    else if (helixesLeft > 0)
                                        AddText("almostAllFragments");
                                    else
                                        AddText("allFragments");
                                }
                                else if (helixes == PlayState.MAX_FRAGMENTS)
                                    AddText("restoredSun");
                                else
                                    AddText("default");
                                break;

                            case 40:
                                if (PlayState.IsBossAlive(1))
                                    AddText("warnAboutStompy");
                                else
                                    AddText("default");
                                break;

                            case 41:
                                if (PlayState.IsBossAlive(0))
                                    AddText("warnAboutSB");
                                else if (!PlayState.CheckForItem(PlayState.Items.Boomerang))
                                    AddText("greyDoor");
                                else if (PlayState.IsBossAlive(3))
                                    AddText(PlayState.currentProfile.character switch { "Snaily" => "babySnails", "Upside" => "babySnails", _ => "goodLuck" });
                                else
                                    AddText("default");
                                break;

                            case 42:
                                if (!PlayState.CheckForItem(PlayState.Items.RapidFire) && PlayState.currentProfile.character != "Leechy" && PlayState.IsBossAlive(2))
                                    AddText("noRapidFire");
                                else if (PlayState.IsBossAlive(2))
                                    AddText("pinkGrass");
                                else
                                    AddText("default");
                                break;

                            case 43:
                                if (!PlayState.CheckForItem(PlayState.Items.FlyShell))
                                    AddText("cantCorner");
                                else if (PlayState.currentProfile.character == "Upside")
                                    AddText("upside");
                                else if (PlayState.currentProfile.character == "Leggy")
                                    AddText("leggy");
                                else if (PlayState.currentProfile.character == "Blobby")
                                    AddText("blobby");
                                else
                                    AddText("default");
                                break;

                            case 45:
                                if (PlayState.isRandomGame && CountItemsInRoom() > 0)
                                    AddText("offerRando");
                                else if (PlayState.isRandomGame)
                                    AddText("emptyRando");
                                else if (PlayState.CheckForItem(PlayState.Items.MetalShell))
                                {
                                    AddText(PlayState.currentProfile.character switch
                                    {
                                        "Sluggy" => "fullPower",
                                        "Blobby" => "nonNeutonian",
                                        "Leechy" => "fullPower",
                                        _ => "fullMetal"
                                    });
                                }
                                else
                                    AddText("default");
                                break;

                            case 46:
                                AddText("default");
                                break;

                            case 47:
                                AddText("default");
                                break;

                            case 48:
                                if (nexted == 0)
                                    AddText("greet");
                                else if (nexted == 1)
                                    AddText("scream");
                                else
                                    AddText("default");
                                break;

                            case 50:
                                AddText("default");
                                break;

                            case 51:
                                if (PlayState.CheckForItem(PlayState.Items.DebugRW))
                                    AddText("admireRainbowWave");
                                else
                                    AddText("default");
                                break;

                            case 52:
                                AddText("default");
                                break;

                            case 53:
                                AddText("default");
                                break;

                            case 54:
                                AddText("default");
                                break;

                            case 55:
                                if (nexted == 0)
                                {
                                    AddText("default");
                                    nexted++;
                                }
                                else
                                    AddText("second");
                                break;

                            case 56:
                                AddText("default");
                                break;

                            default:
                                AddText("?");
                                break;
                        }
                    }
                    if (intentionallyEmpty)
                        return;
                    if (textToSend.Count == 0)
                        textToSend.Add(PlayState.GetText("npc_?"));
                    hasLongDialogue = false;
                    if (textToSend.Count > 1)
                    {
                        if (!speechBubbleSprite.enabled)
                            speechBubbleSprite.enabled = true;
                        if (!PlayState.isTalking)
                            ToggleBubble(true);
                        hasLongDialogue = true;
                        if (Control.SpeakPress())
                        {
                            chatting = true;
                            PlayState.isTalking = true;
                            PlayState.paralyzed = true;
                            PlayState.OpenDialogue(3, ID, textToSend, boxShape, boxColor, portraitStateList, PlayState.player.transform.position.x < transform.position.x);
                            ToggleBubble(false);
                        }
                    }
                    else
                    {
                        chatting = true;
                        PlayState.isTalking = true;
                        PlayState.OpenDialogue(2, ID, textToSend, boxShape, boxColor);
                    }
                }
                else
                    needsSpace = true;
            }
            else if (hasLongDialogue && chatting && !PlayState.dialogueOpen)
            {
                chatting = false;
                needsSpace = false;
                PlayState.isTalking = false;
            }
            else if (Vector2.Distance(transform.position, PlayState.player.transform.position) > 7 && chatting)
            {
                chatting = false;
                PlayState.CloseDialogue();
            }
            else if (Vector2.Distance(transform.position, PlayState.player.transform.position) > 7 && needsSpace)
                needsSpace = false;
            else if (Vector2.Distance(transform.position, PlayState.player.transform.position) > 1.5f && (!chatting || PlayState.paralyzed))
                ToggleBubble(false);

            if (bubbleState && !speechBubbleControl.isActiveAndEnabled && speechBubbleAnim.GetCurrentFrame() >= bubbleControlAppearFrame)
            {
                speechBubbleControl.gameObject.SetActive(true);
                speechBubbleControl.SetIcon(Control.ActionIDToSpriteID(18));
            }

            switch (ID) {
                default:
                    break;
                case 1:
                    if (PlayState.hasJumped && nexted == 0)
                        Next();
                    break;
                case 4:
                    if (!PlayState.IsTileSolid(new Vector2(transform.position.x - 2.5f, transform.position.y)) && nexted == 0)
                        Next();
                    break;
                case 8:
                    if (!PlayState.IsTileSolid(new Vector2(transform.position.x + 8.5f, transform.position.y)) && nexted == 0)
                        Next();
                    break;
                case 16:
                    if (transform.localPosition.y < origin.y - 21 && nexted == 0)
                        Next();
                    break;
                case 48:
                    if (transform.localPosition.y < origin.y - 2 && nexted == 0)
                        Next();
                    if (transform.localPosition.y < origin.y - 35 && nexted == 1)
                        Next();
                    break;
            }
        }
    }

    public virtual void AddText(string textID, string extraInfo = "")
    {
        if (textID == "?" || textID == "")
        {
            textToSend.Add(PlayState.GetText("npc_?"));
            portraitStateList.Add(PlayState.GetTextInfo("npc_?").value);
        }
        else
        {
            bool locatedAll = false;
            int i = 0;
            string baseID;
            string hintItemID = "";
            string hintAreaID = "";
            if (textID.Substring(0, 4) == "HINT")
            {
                int hintID = int.Parse(textID.Split('|')[1]);
                baseID = string.Format("npc_rando_hint_{0}_", PlayState.currentRando.npcHintData[hintID + 1]);
                hintItemID = string.Concat("npc_rando_item_", PlayState.currentRando.npcHintData[hintID + 2] switch
                {
                    4 => PlayState.currentProfile.character == "Blobby" ? "4b" : "4a",
                    5 => PlayState.currentProfile.character == "Blobby" ? "5b" : "5a",
                    6 => PlayState.currentProfile.character == "Leechy" ? "6b" : "6a",
                    8 => PlayState.currentProfile.character switch { "Upside" => "8b", "Leggy" => "8c", "Blobby" => "8d", _ => "8a" },
                    9 => PlayState.currentProfile.character switch { "Sluggy" or "Leechy" => "9b", "Blobby" => "9c", _ => "9a" },
                    _ => PlayState.currentRando.npcHintData[hintID + 2]
                });
                hintAreaID = string.Concat("npc_rando_area_", PlayState.currentRando.npcHintData[hintID + 3].ToString());
            }
            else if (textID == "FLAVOR")
                baseID = string.Format("npc_rando_flavor_{0}_", PlayState.currentRando.npcTextIndeces[ID]);
            else
                baseID = string.Format("npc_{0}_{1}_", ID.ToString(), textID);
            while (!locatedAll)
            {
                string fullID = string.Concat(baseID, i.ToString());
                string newText = PlayState.GetText(fullID);
                if (newText != fullID)
                {
                    if (hintItemID != "")
                    {
                        newText = newText.Replace("[0]", PlayState.GetText(hintItemID));
                        newText = newText.Replace("[1]", PlayState.GetText(hintAreaID));
                    }
                    textToSend.Add(newText);
                    portraitStateList.Add(PlayState.GetTextInfo(fullID).value);
                }
                else
                    locatedAll = true;
                i++;
            }
        }
    }

    private void CreateNewSprites() {
        List<Sprite> newSprites = new();

        int thisID = (ID == 38 && PlayState.CountFragments() < PlayState.MAX_FRAGMENTS) ? 44 : ID;
        for (int i = 0; i < PlayState.textureLibrary.library[Array.IndexOf(PlayState.textureLibrary.referenceList, "Entities/SnailNpc")].Length; i++) {
            newSprites.Add(PlayState.Colorize("Entities/SnailNpc", i, "Entities/SnailNpcColor", thisID));
        }

        sprites = newSprites.ToArray();
    }

    public void Next() {
        nexted++;
        PlayState.CloseDialogue();
        chatting = false;
    }

    public void ToggleBubble(bool state)
    {
        //if (speechBubbleAnim.animList.Count == 0)
        //{
        //    speechBubbleAnim.Add("NPC_bubble_open");
        //    speechBubbleAnim.Add("NPC_bubble_close");
        //}
        if (state && !bubbleState)
        {
            speechBubbleSprite.enabled = true;
            speechBubbleAnim.Play(string.Format("NPC_bubble_open_{0}", upsideDown ? "up" : "down"));
            bubbleState = true;
        }
        else if (!state && bubbleState)
        {
            speechBubbleAnim.Play(string.Format("NPC_bubble_close_{0}", upsideDown ? "up" : "down"));
            bubbleState = false;
            speechBubbleControl.gameObject.SetActive(false);
        }
    }

    //private bool CheckForUncollectedItem(int ID, bool strictIDCheck = false)
    //{
    //    GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
    //    if (items.Length == 0)
    //        return false;
    //    bool foundItem = false;
    //    foreach (GameObject obj in items)
    //    {
    //        Item objScript = obj.GetComponent<Item>();
    //        if (strictIDCheck)
    //        {
    //            if (objScript.itemID == ID)
    //                foundItem = true;
    //        }
    //        else
    //        {
    //            if (objScript.itemID >= PlayState.OFFSET_FRAGMENTS && ID >= PlayState.OFFSET_FRAGMENTS)
    //                foundItem = true;
    //            else if (objScript.itemID >= PlayState.OFFSET_HEARTS && ID >= PlayState.OFFSET_HEARTS)
    //                foundItem = true;
    //            else if (objScript.itemID == ID)
    //                foundItem = true;
    //        }
    //    }
    //    return foundItem;
    //}

    private int CountItemsInRoom()
    {
        Transform room = transform.parent;
        int count = 0;
        for (int i = 0; i < room.childCount; i++)
            if (room.GetChild(i).name.Contains("Item"))
                if (!room.GetChild(i).GetComponent<Item>().collected && room.GetChild(i).GetComponent<Item>().itemID != -1)
                    count++;
        return count;
    }
}
