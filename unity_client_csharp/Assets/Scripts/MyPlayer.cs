using System.Collections;
using UnityEngine;

public class MyPlayer : Player
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private NetworkManager _network;
    void Start()
    {
        StartCoroutine(nameof(CoSendPacket));
        _network = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    IEnumerator CoSendPacket()
    {
        while (true)
        {
            yield return new WaitForSeconds(3.0f);

            C_Move movePacket = new C_Move();
            movePacket.posX = UnityEngine.Random.Range(-50, 50);
            movePacket.posY = 0;
            movePacket.posZ = UnityEngine.Random.Range(-50, 50);


            _network.Send(movePacket.Write());
        }
    }
}
