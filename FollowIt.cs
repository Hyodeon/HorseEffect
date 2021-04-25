using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class FollowIt : MonoBehaviour
{
    // 말의 행동은 총 4단계로 이루어짐.
    // 각각의 상태를 나타내는 코드값들을 저장하고
    public const int STATE_POSITIONING = 10;
    // POSITIONING 단계에서 가장 가까이 있는 사람의 id를 탐색
    public const int STATE_ROTATING = 11;
    // ROTATING 단계에서 간섭없이 회전을 진행
    public const int STATE_FOLLOWING = 12;
    // FOLLOWING 단계에선 인지한 1명의 사람만을 추적함 (탐색을 중단)
    public const int STATE_IDLE = 13;
    // IDLE 단계에선 그 사람에게 도착 후 8초간 (조정가능) 쳐다보는 동작을 수행 후 다시 SEARCHING으로 돌아감.
    public const int STATE_RESTING = 14;
    // 추가적으로 사람이 발견안되면 RESTING으로 가서 alpha를 없애고 사라짐.

    // 플레이어 좌표를 매핑하는 변수 ( 배수 설정 -> 실제 배율에 맞춰야함. )
    public int MapDegree = 13;


    // 각 state별 진행 시간을 결정하는 변수
    public int posi = 3;
    public int rota = 4;
    public int foll = 8;
    public int idle = 10;

    // 이동속도 설정하는 변수
    public float moveSpeed = 5.0f;

    // 자기 자신의 컴포넌트 (위치)를 받아오기 위한 변수
    private Transform m_transform = null;
    // 말의 애니메이션 받아옴
    private Animator anim;

    // 키넥트매니저 인스턴스 받아올 놈
    private KinectManager m_KinectMg = null;

    // Renderer를 받아올 변수들
    private GameObject[] rdrGameObject = new GameObject[3];
    private SkinnedMeshRenderer[] rdr = new SkinnedMeshRenderer[3];

    // 각 키넥트로부터 받은 유저들 위치 저장하는 벡터 동적할당
    private Vector3[] user = new Vector3[3];

    // 말이 쫓아갈 대상 ( 가장 가까운 녀석 )
    static Vector3 Target;

    // Delay 함수에서 사용할 스태틱 변수 제작
    static float delayLength = 0;
    static bool onDelay;

    // Update에 쓸 변수
    private float dist;
    private Vector3 moveDir;
    private bool isDetected = false;

    private double modelAlpha = 0.257;

    // 현재 진행도 / 상태를 나타내는 변수
    public int State = STATE_RESTING;

    // 시간 흐름 카운트 하는 함수 static 방식이라 카운트 잘 됨.
    bool TimeDelaying(bool isTriggered = true, float Sec = 0)
    {
        if (!isTriggered)
            delayLength = Sec;

        if (delayLength <= 0)
            return false;

        delayLength -= Time.deltaTime;

        return true;
    }

    public Vector3 GetPosition()
    {
        return Target;
    }

    // Start is called before the first frame update
    void Start()
    {
        m_transform = this.gameObject.GetComponent<Transform>();
        anim = this.gameObject.GetComponent<Animator>();

        for (int i = 1; i < 4; i++)
        {
            rdrGameObject[i - 1] = this.transform.GetChild(i).gameObject;
            rdr[i - 1] = rdrGameObject[i - 1].GetComponent<SkinnedMeshRenderer>();
        }

        //m_KinectMg = KinectManager.Instance;

        anim.SetBool("isWalk", false);
    }

    // Update is called once per frame
    void Update()
    {
        switch (State)
        {
            case STATE_POSITIONING: // 사람을 찾는 상태 ====================================================================

                TimeDelaying(TimeDelaying(), posi);

                //Target = m_KinectMg.GetUserPosition(m_KinectMg.GetUserIdByIndex(0)) * MapDegree;

                moveDir = (Target.x - m_transform.position.x) * Vector3.forward;

                /*
                if (m_KinectMg.IsUserDetected())
                    isDetected = true;
                */

                if (!TimeDelaying() && isDetected)
                    State++;
                else
                    State = STATE_RESTING;


                break;

            //==============================================================================================================

            case STATE_ROTATING: // 찾은 후 말 방향 회전 ====================================================================

                TimeDelaying(TimeDelaying(), rota);

                dist = Mathf.Abs(Target.x - m_transform.position.x);

                m_transform.rotation = Quaternion.LookRotation(Vector3.right * (Target.x - m_transform.position.x));

                if (!TimeDelaying())
                {
                    State++;

                    if (Vector3.Dot(Vector3.right * (Target.x - m_transform.position.x), Vector3.right) < 0)
                        moveDir = -moveDir;
                }




                break;

            //==============================================================================================================

            case STATE_FOLLOWING: // 찾은 사람을 따라감 =====================================================================

                TimeDelaying(TimeDelaying(), foll);
                anim.SetBool("isWalk", true);
                dist = Mathf.Abs(Target.x - m_transform.position.x);

                moveSpeed = dist / 8 * 1.3f;

                m_transform.Translate(moveDir.normalized * moveSpeed * Time.deltaTime);

                if (!TimeDelaying())
                    State++;




                break;

            //==============================================================================================================

            case STATE_IDLE: // 따라가서 멈추고 사람을 바라봄 ================================================================

                TimeDelaying(TimeDelaying(), idle);
                anim.SetBool("isWalk", false);
                if (!TimeDelaying())
                    State = STATE_POSITIONING;




                break;


            //==============================================================================================================

            case STATE_RESTING: // 사람 탐지 X라서 쉼 =======================================================================

                TimeDelaying(TimeDelaying(), 3);
                anim.SetBool("isWalk", false);

                /*
                if (!TimeDelaying() && m_KinectMg.IsUserDetected())
                    State = STATE_POSITIONING;

                if (modelAlpha >= 0)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        rdr[i].materials[0].color = new Color(rdr[i].materials[0].color.r, rdr[i].materials[0].color.g,
                            rdr[i].materials[0].color.b, (float)modelAlpha);
                    }
                    modelAlpha -= 0.01;
                }
                */

                break;
        }

    }
}
