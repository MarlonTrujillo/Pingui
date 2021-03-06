﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SgLib;

public class PlayerController : MonoBehaviour
{
    public static event System.Action PlayerDied;

    [Header("Gameplay References")]
    public UIManager uIManager;
    public CameraController cameraController;
    //public GameObject laserPointPrefab;
    public GameObject trailPrefab;

    [Header("Gameplay Config")]
    [SerializeField]
    private float normalSpeed = 10f;
    [SerializeField]
    private float maxSpeed = 60f;
    [SerializeField]
    private float turnSpeed = 10f;
    [SerializeField]
    private float turnAngle = 30f;
    [SerializeField]
    private float speedIncreaseFactor = 1f;

    public float speed { private set; get; }

    public bool inLaserMode { private set; get; }

    public bool inMagnetMode { private set; get; }

    private List<GameObject> listLaserPoint = new List<GameObject>();
    private GameObject trail;
    private GameObject trail2;

    // Variables para el spherecast
    private Vector3 sphereDirection;
    private Vector3 sphereOrigin;

    private GameObject currentObjectHittedByLaser = null;
    private RaycastHit hit;
    private Renderer render;

    private float rotateSpeed;
    private float timeCount = 0;
    private bool disableCheckGameOver = false;
    private bool disableTouch = false;
    public bool isFixedRotation = false;
    public bool stopFixRotation = false;

    private Coroutine laserModeCoroutine;
    private Coroutine magnetModeCoroutine;

    void Start()
    {
        //        // Uncomment to enable changing the character to the selected one
        GameObject currentCharacter = CharacterManager.Instance.characters[CharacterManager.Instance.CurrentCharacterIndex];
        Mesh charMesh = currentCharacter.GetComponent<MeshFilter>().sharedMesh;
        Material charMaterial = currentCharacter.GetComponent<Renderer>().sharedMaterial;
        GetComponent<MeshFilter>().mesh = charMesh;
        GetComponent<MeshCollider>().sharedMesh = charMesh;
        GetComponent<Renderer>().material = charMaterial;

        sphereOrigin = transform.position + new Vector3(0, 0, 0f);

        #region OLD TRAIL
       
        //Create the trail and laserpoints
        for (int i = 0; i < currentCharacter.transform.childCount; i++)
        {
            GameObject child = currentCharacter.transform.GetChild(i).gameObject;
            if (child.CompareTag("Trail"))
            {
                // No es útil por el momento
                /*
                trail = Instantiate(trailPrefab, transform.position, Quaternion.identity);
                trail2 = Instantiate(trailPrefab, transform.position, Quaternion.identity);
                trail.transform.SetParent(transform);
                trail2.transform.SetParent(transform);
                trail.transform.localPosition = child.transform.localPosition + new Vector3(0.2f, 0.5f, 0.2f);
                trail2.transform.localPosition = child.transform.localPosition + new Vector3(0.2f, 0.5f, -0.2f);
                */
            }
      
            #endregion

            #region OLD LASER POINTS
            // Ya no se usan los laser points
            /*else
            {
                GameObject laserPoint = Instantiate(laserPointPrefab, transform.position, Quaternion.identity);
                laserPoint.transform.SetParent(transform);
                laserPoint.transform.localPosition = child.transform.localPosition;
                listLaserPoint.Add(laserPoint);
                LineRenderer lineRender = laserPoint.GetComponent<LineRenderer>();
                lineRender.SetPosition(0, Vector3.zero);
                lineRender.SetPosition(1, Vector3.right * GameManager.Instance.laserLength);
                laserPoint.SetActive(false);
            }*/
            #endregion
        }

        render = GetComponent<Renderer>();

        //Setup speed and rotate speed
        speed = normalSpeed;
        rotateSpeed = turnSpeed * 5f;
    }

    // Update is called once per frame
    void Update()
    {
        //Moving 
        if (GameManager.Instance.GameState == GameState.Playing)
        {
            transform.position += Vector3.right * speed * Time.deltaTime;
#if UNITY_EDITOR
            if (Input.GetKey(KeyCode.A) && !disableTouch)
            {
                float z = transform.position.z;
                if (z < 6f)
                {
                    isFixedRotation = false;
                    stopFixRotation = true;
                    transform.position += new Vector3(0, 0, turnSpeed * Time.deltaTime);
                    if (timeCount < turnAngle)
                    {
                        timeCount += rotateSpeed * Time.deltaTime;
                        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, timeCount);
                    }
                }
            }
            else if (Input.GetKey(KeyCode.D) && !disableTouch)
            {
                float z = transform.position.z;
                if (z > -6f)
                {
                    isFixedRotation = false;
                    stopFixRotation = true;
                    transform.position -= new Vector3(0, 0, turnSpeed * Time.deltaTime);
                    if (timeCount > -turnAngle)
                    {
                        timeCount -= rotateSpeed * Time.deltaTime;
                        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, timeCount);
                    }
                }
            }
            else
            {
                stopFixRotation = false;
                if (!isFixedRotation)
                    StartCoroutine(FixRotation());
            }
#endif
            if (Input.GetMouseButton(0) && !disableTouch)
            {

                float x = Camera.main.ScreenToViewportPoint(Input.mousePosition).x;
                float z = transform.position.z;
                if (x <= 0.5f)//Turn left
                {
                    if (z < 6f)
                    {
                        isFixedRotation = false;
                        stopFixRotation = true;
                        transform.position += new Vector3(0, 0, turnSpeed * Time.deltaTime);
                        if (timeCount < turnAngle)
                        {
                            timeCount += rotateSpeed * Time.deltaTime;
                            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, timeCount);
                        }
                    }
                }
                else
                {
                    if (z > -6f)
                    {
                        isFixedRotation = false;
                        stopFixRotation = true;
                        transform.position -= new Vector3(0, 0, turnSpeed * Time.deltaTime);
                        if (timeCount > -turnAngle)
                        {
                            timeCount -= rotateSpeed * Time.deltaTime;
                            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, timeCount);
                        }
                    }
                }
            }
            if (Input.GetMouseButtonUp(0))
            {
                stopFixRotation = false;
                if (!isFixedRotation)
                    StartCoroutine(FixRotation());
            }

            #region OLD LASER MECHANIC
            /* 
            //In laser mode
            if (inLaserMode)
            {
                speed = maxSpeed;

                ////Draw the ray to check the object ahead
                int length = listLaserPoint.Count;
                for (int i = 0; i < length; i++)
                {
                    Ray rayAhead = new Ray(listLaserPoint[i].transform.position, Vector3.right);
                    if (Physics.Raycast(rayAhead, out hit, GameManager.Instance.laserLength))
                    {
                        if (hit.collider.CompareTag("Obstacle"))
                        {
                            if (currentObjectHittedByLaser != hit.collider.gameObject)
                            {
                                currentObjectHittedByLaser = hit.collider.gameObject;
                                ResetObjectHittedByLaser(hit.collider.gameObject, hit.point);
                            }
                        }
                    }
                }
            }
            */
            #endregion

            // New Laser Mode - Invulnerability

            if (inLaserMode)
            {
                speed = maxSpeed;
                sphereOrigin = transform.position + new Vector3(0, 0f, 0f);
                sphereDirection = transform.position;

                if (Physics.SphereCast(sphereOrigin, GameManager.Instance.invincibleRadius, sphereDirection, out hit))
                {
                    if (hit.collider.CompareTag("Obstacle"))
                    {
                        if (currentObjectHittedByLaser != hit.collider.gameObject)
                        {
                            currentObjectHittedByLaser = hit.collider.gameObject;
                            ResetObjectHittedByLaser(hit.collider.gameObject, hit.point);
                        }
                    }
                }
             
            }


            else //Not in laser mode
            {
                speed = normalSpeed;
                if (!disableCheckGameOver)
                    CheckPlayerFallDown();
            }
        }
    }

    private void OnDrawGizmosSelected() 
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(sphereOrigin, GameManager.Instance.invincibleRadius);
    }

    //Fix player rotation
    IEnumerator FixRotation()
    {
        
        if (timeCount > 0)
        {
            while (timeCount > 0)
            {
                if (stopFixRotation)
                    yield break;
                timeCount -= rotateSpeed * Time.deltaTime;
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, timeCount);
                yield return null;
            }
        }
        else
        {
            while (timeCount < 0)
            {
                if (stopFixRotation)
                    yield break;
                timeCount += rotateSpeed * Time.deltaTime;
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, timeCount);
                yield return null;
            }
        }
        isFixedRotation = true;
        transform.eulerAngles = new Vector3 (50, 90, 0);
        //yield return null; // borrar cuando se descomente lo de arriba
    }


    //Reset the object hitted by the laser
    void ResetObjectHittedByLaser(GameObject ob, Vector3 hitPoint)
    {
        //Play particle
        GameManager.Instance.ObstacleExplore(ob.transform.position);
        ScoreManager.Instance.AddScore(1);
        if (ob.transform.parent != null)
            ob.SetActive(false);
        else
        {
            ob.transform.eulerAngles = Vector3.zero;
            ob.transform.position = Vector3.zero;
            ob.transform.SetParent(GameManager.Instance.transform);
            ob.SetActive(false);
        }
    }

    void SetLaserState(bool activeState)
    {
        foreach (GameObject o in listLaserPoint)
        {
            o.SetActive(activeState);
        }
    }

    // Calls this when the player dies and game over
    void Die(float disappearAfter = -1)
    {
        // Fire event
        PlayerDied();

        if (disappearAfter >= 0)
            StartCoroutine(CRDisappear(disappearAfter));
    }

    IEnumerator CRDisappear(float after)
    {
        yield return new WaitForSeconds(after);
        gameObject.SetActive(false);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance.GameState == GameState.Playing)
        {
            if (other.CompareTag("Obstacle")) //Hit obstacle -> die
            {
                // If player has 1 or more armor, armor will reduce
                if (GameManager.Instance.armorUnits > 0)
                {
                    GameManager.Instance.armorUnits--;
                    cameraController.ShakeCamera();
                    SoundManager.Instance.PlaySound(SoundManager.Instance.hit, true);
                }
                // if armor is 0 player dies
                else
                {
                    Die(0.2f);
                    cameraController.ShakeCamera();
                    GameManager.Instance.PlayerExplodes(transform.position, 0f);
                    uIManager.HideLaserModeTime();
                    uIManager.HideMagnetModeTime();
                }
               
            }
            else //Hit items
            {
                ItemController itemControl = other.GetComponent<ItemController>();
                if (itemControl.itemType == ItemType.LASER) //Laser item
                {
                    inLaserMode = true;
                    SetLaserState(true);
                    if (laserModeCoroutine != null)
                        StopCoroutine(laserModeCoroutine);
                    laserModeCoroutine = StartCoroutine(CountDownLaserModeTime());
                }
                else if (itemControl.itemType == ItemType.MAGNET) //Magnet item
                {
                    inMagnetMode = true;
                    if (magnetModeCoroutine != null)
                        StopCoroutine(magnetModeCoroutine);
                    magnetModeCoroutine = StartCoroutine(CountDownMagnetModeTime());
                }
            }
        }
    }

    IEnumerator CountDownLaserModeTime()
    {
        uIManager.SetLaserImgValue(1);
        uIManager.ShowLaserModeTime();
        float delaytime = GameManager.Instance.laserModeTime;
        float time = 0;
        while (time < delaytime)
        {
            time += Time.deltaTime;
            float factor = time / delaytime;
            float value = Mathf.Lerp(1, 0, factor);
            uIManager.SetLaserImgValue(value);
            yield return null;
        }
        uIManager.HideLaserModeTime();
        inLaserMode = false;
        SetLaserState(false);
        laserModeCoroutine = null;
    }

    IEnumerator CountDownMagnetModeTime()
    {
        uIManager.SetMagnetImgValue(1);
        uIManager.ShowMagnetModeTime();
        float delaytime = GameManager.Instance.laserModeTime;
        float time = 0;
        while (time < delaytime)
        {
            time += Time.deltaTime;
            float factor = time / delaytime;
            float value = Mathf.Lerp(1, 0, factor);
            uIManager.SetMagnetImgValue(value);
            yield return null;
        }
        inMagnetMode = false;
        magnetModeCoroutine = null;
    }

    void CheckPlayerFallDown()
    {
        Vector3 checkPos = transform.position + Vector3.right * render.bounds.size.x / 2f;
        Ray rayDown = new Ray(checkPos, Vector3.down);
        RaycastHit hit;
        if (!Physics.Raycast(rayDown, out hit, 5f))
        {
            uIManager.HideLaserModeTime();
            uIManager.HideMagnetModeTime();
            disableTouch = true;
            StartCoroutine(Falling());
        }
    }

    IEnumerator Falling()
    {
        yield return null;
        Die();
        Vector3 fallingDir = Vector3.right + Vector3.down;
        while (true)
        {
            transform.position += fallingDir * speed / 2f * Time.deltaTime;
            transform.eulerAngles += new Vector3(0, 0, -Time.deltaTime * speed * 2f);
            yield return null;
        }
    }

    public void IncreaseSpeed()
    {
        normalSpeed += speedIncreaseFactor;
        maxSpeed += speedIncreaseFactor;
    }
}
