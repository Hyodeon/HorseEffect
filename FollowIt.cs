using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class FollowIt: MonoBehaviour
{
    // 말의 행동은 총 4단계로 이루어짐.
    // 각각의 상태를 나타내는 코드값들을 저장하고
    public const int STATE_POSITIONING     = 10;
    // POSITIONING 단계에서 가장 가까이 있는 사람의 id를 탐색
    public const int STATE_ROTATING = 11;
    // ROTATING 단계에서 간섭없이 회전을 진행
    public const int STATE_FOLLOWING = 12;
    // FOLLOWING 단계에선 인지한 1명의 사람만을 추적함 (탐색을 중단)
    public const int STATE_IDLE = 13;
    // IDLE 단계에선 그 사람에게 도착 후 8초간 (조정가능) 쳐다보는 동작을 수행 후 다시 SEARCHING으로 돌아감.


    public int MapDegree = 13;
    public int posi = 3;
    public int rota = 4;
    public int foll = 8;
    public int idle = 10;

    public float moveSpeed = 5.0f;

    // 자기 자신의 컴포넌트 (위치)를 받아오기 위한 변수
    private Transform m_transform = null;
    private Animator anim;

    private KinectManager m_KinectMg = null;

    private Vector3[] user = new Vector3[3];

    static Vector3 Target;

    // Delay 함수에서 사용할 스태틱 변수 제작
    static float delayLength = 0;
    static bool onDelay;

    // Update에 쓸 변수
    private float dist;
    private Vector3 moveDir;

    // 현재 진행도 / 상태를 나타내는 변수
    public int State = STATE_POSITIONING;

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

        m_KinectMg = KinectManager.Instance;

        anim.SetBool("isWalk", false);
    }

    // Update is called once per frame
    void Update()
    {

        //Target = m_KinectMg.GetUserPosition(m_KinectMg.GetUserIdByIndex(0)) * 10;

        // 각 벡터들을 이용하여 움직여줌

        switch (State)
        {
            case STATE_POSITIONING:
                TimeDelaying(TimeDelaying(), posi);

                Target = m_KinectMg.GetUserPosition(m_KinectMg.GetUserIdByIndex(0)) * MapDegree;

                moveDir = (Target.x - m_transform.position.x) * Vector3.forward;

                if (!TimeDelaying())
                    State++;
                break;

            case STATE_ROTATING:
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

            case STATE_FOLLOWING:
                TimeDelaying(TimeDelaying(), foll);
                anim.SetBool("isWalk", true);
                dist = Mathf.Abs(Target.x - m_transform.position.x);

                moveSpeed = dist / 8 * 1.3f;

                m_transform.Translate(moveDir.normalized * moveSpeed * Time.deltaTime);

                if (!TimeDelaying())
                    State++;
                break;

            case STATE_IDLE:
                TimeDelaying(TimeDelaying(), idle);
                anim.SetBool("isWalk", false);
                if (!TimeDelaying())
                    State = STATE_POSITIONING;
                break;
        }
        
    }
}
