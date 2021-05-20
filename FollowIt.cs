using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using com.rfilkov.kinect;




public class FollowIt : MonoBehaviour
{

    public struct REGION_
    {
        public float LeftEnd;
        public float RightEnd;
    };

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
    public int MapDegree = 10;


    // 각 state별 진행 시간을 결정하는 변수
    public int POSITIONING_TIME = 3;
    public int ROTATION_TIME = 4;
    public int FOLLOWING_TIME = 7;
    public int IDLE_TIME = 8;
    public int RESTING_TIME = 3;

    // 이동속도 설정하는 변수
    public float moveSpeed = 5.0f;

    // 자기 자신의 컴포넌트 (위치)를 받아오기 위한 변수
    private Transform m_transform = null;
    // 말의 애니메이션 받아옴
    private Animator anim;
    //private lookAnimater lAnim;

    // 키넥트매니저 인스턴스 받아올 놈
    private KinectManager m_KinectMg = null;

    // 각 키넥트로부터 받은 유저들 위치 저장하는 벡터 동적할당
    private Vector3[] user = new Vector3[3];

    // 말이 쫓아갈 대상 ( 가장 가까운 녀석 )
    static Vector3 Target;

    // Delay 함수에서 사용할 스태틱 변수 제작
    public float delay = 0;
    bool isDoneOnce = false;
    bool pending = false;
    bool isStillDetected = false;

    // Update에 쓸 변수
    public float dist;
    private Vector3 moveDir;
    private bool isDetected = false;
    public float animSpeed = 1;
    private bool isFliped = false;

    private double modelAlpha = 0.257;

    // 현재 진행도 / 상태를 나타내는 변수
    public int State = STATE_POSITIONING;

    public Vector3 GetPosition()
    {
        return Target;
    }

    REGION_ GetRegionByTarget(Transform tr, float size_)
    {
        REGION_ temp;

        temp.LeftEnd = tr.position.x - size_;
        temp.RightEnd = tr.position.y + size_;

        return temp;
    }

    private void toNextMode(bool isTriggered)
    {
        State++;
        isDoneOnce = false;
        pending = false;

        if (State == STATE_ROTATING)
        {
            if (Vector3.Dot(Vector3.right * (Target.x - m_transform.position.x), Vector3.right) < 0)
                moveDir = -moveDir;
        }
        else if (State == STATE_FOLLOWING)
            anim.SetBool("isWalk", false);
        else if (State == STATE_IDLE)
            State = STATE_POSITIONING;
    }

    private float min_to_max(float input, float _min, float _max)
    {
        float calc = input;

        if (calc >= _max)
            calc = _max;
        else if (calc <= _min)
            calc = _min;

        return calc;
    }

    // Start is called before the first frame update
    void Start()
    {

        m_transform = this.gameObject.GetComponent<Transform>();
        anim = this.gameObject.GetComponent<Animator>();

        m_KinectMg = KinectManager.Instance;
        //lAnim = this.GameObject.GetComponent<lookAnimater>;

        anim.SetBool("isWalk", false);
    }


    // Update is called once per frame
    void Update()
    {
        if (!pending)
        {
            if (State == STATE_POSITIONING)
                delay = POSITIONING_TIME;
            else if (State == STATE_ROTATING)
                delay = ROTATION_TIME;
            else if (State == STATE_FOLLOWING)
                delay = FOLLOWING_TIME;
            else if (State == STATE_IDLE)
                delay = IDLE_TIME;
            else if (State == STATE_RESTING)
                delay = RESTING_TIME;

            pending = true;
        }

        switch (State)
        {
            case STATE_POSITIONING: // 사람을 찾는 상태 ====================================================================

                toNextMode(delay <= 0 && isDoneOnce); // 만약 조건이 맞으면 다음 모드로 넘어감

                Target = m_KinectMg.GetUserPosition(m_KinectMg.GetUserIdByIndex(0)) * MapDegree;

                moveDir = (Target.x - m_transform.position.x) * Vector3.forward;

                if (m_KinectMg.IsUserDetected(0))
                    isDetected = true;

                delay -= Time.deltaTime;
                isDoneOnce = true;

                break;

            //==============================================================================================================

            case STATE_ROTATING: // 찾은 후 말 방향 회전 ==ol==================================================================


                toNextMode(delay <= 0 && isDoneOnce);

                dist = Mathf.Abs(Target.x - m_transform.position.x);

                if(isDetected)
                    m_transform.rotation = Quaternion.LookRotation(Vector3.right * (Target.x - m_transform.position.x));

                isDoneOnce = true;

                delay -= Time.deltaTime;


                break;

            //==============================================================================================================

            case STATE_FOLLOWING: // 찾은 사람을 따라감 =====================================================================

                if(dist <= 2f)
                    anim.SetBool("isWalk", false);

                toNextMode((delay <= 0 || dist <= 2f) && isDoneOnce);


                dist = Mathf.Abs(Target.x - m_transform.position.x);

                animSpeed = min_to_max(dist / 6f - 0.3f, 0.5f, 1f);
                anim.SetFloat("walkspeed", animSpeed);


                moveSpeed = dist / 10 * 1.3f;


                m_transform.Translate(moveDir.normalized * moveSpeed * Time.deltaTime);



                isDoneOnce = true;
                delay -= Time.deltaTime;

                anim.SetBool("isWalk", true);

                break;

            //==============================================================================================================

            case STATE_IDLE: // 따라가서 멈추고 사람을 바라봄 ================================================================

                isStillDetected = false;

                toNextMode(delay <= 0 && isDoneOnce && !isStillDetected);

                delay -= Time.deltaTime;

                isDoneOnce = true;

                break;


            //==============================================================================================================

            case STATE_RESTING: // 사람 탐지 X라서 쉼 =======================================================================

                /*
                if (!((GetRegionByTarget(headTargetObject.transform, 1.2f).LeftEnd
                    <= m_KinectMg.GetUserPosition(m_KinectMg.GetUserIdByIndex(0)).x) &&
                    (m_KinectMg.GetUserPosition(m_KinectMg.GetUserIdByIndex(0)).x
                    >= GetRegionByTarget(headTargetObject.transform, 1.2f).RightEnd)))
                    State = STATE_POSITIONING;

                */
                break;
        }

    }
}
