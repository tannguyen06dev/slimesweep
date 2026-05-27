using UnityEngine;



public class SlimeBounce : MonoBehaviour

{

    public float speed = 9f; // tốc độ nhún

    public float bounceHeight = 0.1f; // độ nhún

    public float moveThreshold = 0.01f; // độ nhạy phát hiện di chuyển



    private Vector3 baseScale;

    private Vector3 lastPosition;



    void Start()

    {

        baseScale = transform.localScale;

        lastPosition = transform.position;

    }



    void Update()

    {

        // Tính vận tốc hiện tại (khoảng cách di chuyển giữa 2 frame)

        float moveDistance = (transform.position - lastPosition).magnitude;



        // Nếu slime đang di chuyển → nhún, ngược lại giữ scale bình thường

        if (moveDistance > moveThreshold)

        {

            float scaleY = 1 + Mathf.Sin(Time.time * speed) * bounceHeight;

            transform.localScale = new Vector3(1 / scaleY, scaleY, 1 / scaleY);

        }

        else

        {

            transform.localScale = Vector3.Lerp(transform.localScale, baseScale, Time.deltaTime * 10f);

        }



        // Cập nhật vị trí cuối cùng

        lastPosition = transform.position;

    }

}