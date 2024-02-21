using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Antymology.Terrain;
using UnityEngine;


public class antLogic : MonoBehaviour {
    public GameObject ant;

    public float health = 100.0f;
    public float healthReduc = 1.0f;
    
    private float jumpHeightForce = 5f; // This controls how high the ant jumps.
    private float jumpDistanceForce = 3f; // This controls how far forward the ant jumps.
    
    private float jumpCooldown = 0.0f;

    private int north = 0;
    private int east = 0;
    private int south = 0;
    private int west = 0;

    private int facing = 0;
        


    private Vector3 position;
    
    // WorldManager
    private WorldManager worldManager;
    private AbstractBlock blockBelow;
    private Vector3 blockBelowPos;
    

    void Start() {
        facing = north;
        ant = this.gameObject;
        worldManager = GameObject.Find("Manager").GetComponent<WorldManager>();

    }

    // Update is called once per frame
    void Update() {
        position = ant.transform.position;

        // keep backing up until it's not touching the wall in front of it.
        backUp();
        calculateBlockBelow();
        
        // debugging
        DrawDebugForwardLine();
        
        if (jumpCooldown > 0.0f) {
            jumpCooldown -= Time.deltaTime;
        }
        else {
            jump();
            jumpCooldown = 1.5f;
            
        }
        
        if (blockBelow is MulchBlock && health <= 93.0f) {
            consume();
        }
        
        loseHealth();
    }
    
    private void jump() {
        int power = jumpPossible();
        Rigidbody rb = ant.GetComponent<Rigidbody>();
        
        adjustForces(power);

        if (power == -1) {
            rotateAnt(rb);

        } else if (power == 0) {
            rb.AddForce(rb.transform.forward * jumpDistanceForce, ForceMode.Impulse);
            
        } else {
            rb.AddForce(rb.transform.up * jumpHeightForce, ForceMode.Impulse);
            StartCoroutine(ForwardAtApex(rb, jumpDistanceForce));
            
        }
    }
    
    private IEnumerator ForwardAtApex(Rigidbody rb, float force) {
        yield return new WaitUntil(() => rb.velocity.y > 0);
        yield return new WaitUntil(() => Mathf.Abs(rb.velocity.y) < 0.1f);
        rb.AddForce(rb.transform.forward * force, ForceMode.Impulse);
    }

    private void rotateAnt(Rigidbody rb) {
        rb.constraints &= ~RigidbodyConstraints.FreezeRotationY;
        ant.transform.Rotate(0, 90, 0);
        rb.constraints = RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void adjustForces(int power) {
        if (power == 0) {
            jumpHeightForce = 0.0f;
            jumpDistanceForce = 3f;
            
        }else if (power == 1) {
            jumpHeightForce = 5.0f;
            jumpDistanceForce = 3f;
            
        }else if (power == 2) {
            jumpHeightForce = 9.0f;
            jumpDistanceForce = 1.7f;
            
        }else {
            jumpHeightForce = 0f;
            jumpDistanceForce = 0f;
        }

    }
    
    private int jumpPossible() {
        Vector3Int blockPos = Vector3Int.RoundToInt(ant.transform.position + ant.transform.forward);
        
        bool[] blockInFront = {
            worldManager.GetBlock(blockPos.x, blockPos.y, blockPos.z) is AirBlock,
            worldManager.GetBlock(blockPos.x, blockPos.y + 1, blockPos.z) is AirBlock,
            worldManager.GetBlock(blockPos.x, blockPos.y + 2, blockPos.z) is AirBlock,
            
        };

        if (blockInFront[0] && blockInFront[1]) return 0;
        if (!blockInFront[0] && blockInFront[1]) return 1;
        if (!blockInFront[0] && !blockInFront[1] && blockInFront[2]) return 2;
        return -1;
    }



    private void consume() {
        if (blockBelow is MulchBlock) {
            health = Mathf.Max(health + 10.0f,  100.0f);
            worldManager.SetBlock((int)blockBelowPos.x, (int)blockBelowPos.y, (int)blockBelowPos.z, new AirBlock());
        }
    }
    
    private void loseHealth() {
        float baseLoss = healthReduc * Time.deltaTime;
        health -=  (blockBelow is AcidicBlock) ? (baseLoss * 2) : (baseLoss);
    }
    
    private void backUp() {
        if (Physics.Raycast(ant.transform.position, ant.transform.forward, 0.50f)) {
            ant.transform.position -= ant.transform.forward * 0.1f;
            backUp();
        }
    }

    private void calculateBlockBelow() {
        // Directly calculate the block position below the ant by rounding down the ant's position
        int x = Mathf.RoundToInt(ant.transform.position.x);
        int y = Mathf.RoundToInt(ant.transform.position.y - 0.65f); // Adjust for the distance below the ant we want to check
        int z = Mathf.RoundToInt(ant.transform.position.z);

        // Ensure y is not below the world (assuming y=0 is the lowest)
        y = Mathf.Max(y, 0);

        blockBelow = worldManager.GetBlock(x, y, z);
        blockBelowPos = new Vector3(x, y, z);

        if (blockBelow != null) {
            DrawDebugRectangle(new Vector3(x, y + 0.5f, z)); // Adjust y to visualize the block's top surface
        }
    }


    
    private void DrawDebugRectangle(Vector3 blockPosition) {
        float blockSize = 1.0f; // Assuming each block is 1 unit in size.
        Vector3 halfSize = new Vector3(blockSize / 2, 0, blockSize / 2);

        // Calculate the corners of the block
        Vector3 topLeft = blockPosition + new Vector3(-halfSize.x, 0, halfSize.z);
        Vector3 topRight = blockPosition + new Vector3(halfSize.x, 0, halfSize.z);
        Vector3 bottomLeft = blockPosition + new Vector3(-halfSize.x, 0, -halfSize.z);
        Vector3 bottomRight = blockPosition + new Vector3(halfSize.x, 0, -halfSize.z);

        // Draw lines between the corners to form a rectangle
        Debug.DrawLine(topLeft, topRight, Color.red, 0.1f, false);
        Debug.DrawLine(topRight, bottomRight, Color.red, 0.1f, false);
        Debug.DrawLine(bottomRight, bottomLeft, Color.red, 0.1f, false);
        Debug.DrawLine(bottomLeft, topLeft, Color.red, 0.1f, false);
    }
    
    // draw debug line to show forward ant direction
    private void DrawDebugForwardLine() {
        Debug.DrawLine(ant.transform.position, ant.transform.position + ant.transform.forward, Color.red, 0.1f, false);
    }
}
