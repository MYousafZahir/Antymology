using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Antymology.Terrain;
using UnityEngine;
using Debug = UnityEngine.Debug;


public class antLogic : MonoBehaviour {
    public GameObject ant;

    public float health = 100.0f;
    public float healthReduc = 4.0f;
    
    private float jumpHeightForce = 5f; // This controls how high the ant jumps.
    private float jumpDistanceForce = 3f; // This controls how far forward the ant jumps.
    
    private float jumpCooldown = 0.0f;
    private float hopCooldown = 0.0f;
    private float directionCooldown = 0.0f;

    private bool pickOrDrop = false;
    private float timeBetweenPickandDrop = 4.0f;
    
    private int facing = 0;
        


    private Vector3 position;
    private Vector3 averageNestDirection;
    
    // WorldManager
    private WorldManager worldManager;
    private AbstractBlock blockBelow;
    private Vector3 blockBelowPos;
    private AbstractBlock heldBlock;
    private bool isQueen;

    public List<Vector3> nestPositions = new List<Vector3>();

    void Start() {
        ant = this.gameObject;
        worldManager = GameObject.Find("Manager").GetComponent<WorldManager>();

        if (this.tag == "Ant") isQueen = false;
        if (this.tag == "Queen") isQueen = true;

        heldBlock = null;
        
        // make queen ant pink
        if (isQueen) {
            ant.GetComponent<Renderer>().material.color = Color.magenta;
            averageNestDirection = Vector3.zero;
        }

        StartCoroutine(RoutineJump());
        StartCoroutine(RoutineNestRefresh());
        StartCoroutine(RoutineBlockBehaviour());
        StartCoroutine(RoutineUnstuck());

    }

    // Update is called once per frame
    void Update() {
        position = ant.transform.position;
        

        // keep backing up until it's not touching the wall in front of it.
        backUp();
        checkDeath();
        calculateBlockBelow();
        //directionSet();
        
        
        // debugging
        DrawDebugForwardLine();
        
        // wiggle
        if (blockBelow == null) {
            // add a small force in a random direction
            ant.GetComponent<Rigidbody>().AddForce(new Vector3(Random.Range(-0.3f, 0.3f), 0, Random.Range(-0.3f, 0.3f)), ForceMode.Impulse);
        }
        
        if (blockBelow is MulchBlock && health <= 90.0f) {
            consume();
        }

        if (isQueen && health >= 34.0f) {
            createNest();
        }
        
        loseHealth();
        UnstuckFall();  
    }
    
    IEnumerator RoutineBlockBehaviour() {
        while (true) {
            yield return new WaitForSeconds(2.0f);
            pickUpBlock();

            yield return new WaitForSeconds(1.5f);
            dropBlock();

        }
    }
    
    IEnumerator RoutineJump() {
        while (true) {
            yield return new WaitForSeconds(0.8f);
            jump();
        }
    }
    
    IEnumerator RoutineUnstuck() {
        while (true) {
            Vector3 lastPosition = ant.transform.position;
            yield return new WaitForSeconds(5.0f);
            if (lastPosition == ant.transform.position) {
                ant.transform.position = new Vector3(ant.transform.position.x, ant.transform.position.y + 10, ant.transform.position.z);
            }
        }
    }

    IEnumerator RoutineNestRefresh() {
        while (true) {
            yield return new WaitForSeconds(5f);
            if (isQueen) {
                averageNestDirection = calculateAverageDirectionOfNest();
            }
            else {
                averageNestDirection = GameObject.FindGameObjectWithTag("Queen").GetComponent<antLogic>()
                    .averageNestDirection;
            }

            averageNestDirection = averageNestDirection +
                                   new Vector3(Random.Range(-10.0f, 10.0f), 0, Random.Range(-10.0f, 10.0f));
            
            Rigidbody rb = ant.GetComponent<Rigidbody>();
            rb.constraints &= ~RigidbodyConstraints.FreezeRotationY;
            Vector3 direction;

            if (heldBlock is null) {
                direction = averageNestDirection - ant.transform.position;
            } else {
                direction = ant.transform.position - averageNestDirection;
            }

            // randomize direction a bit, not on y axis though
            direction = new Vector3(direction.x + Random.Range(-5.0f, 5.0f), 0, direction.z + Random.Range(-5.0f, 5.0f));
            ant.transform.forward = new Vector3(direction.x, 0, direction.z);
            rb.constraints = RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

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

    private void hopAndDrop() {
        Rigidbody rb = ant.GetComponent<Rigidbody>();
        StartCoroutine(DropBlockAtApex(rb));
    }
    
    private IEnumerator ForwardAtApex(Rigidbody rb, float force) {
        yield return new WaitUntil(() => rb.velocity.y > 0);
        yield return new WaitUntil(() => Mathf.Abs(rb.velocity.y) < 0.1f);
        rb.AddForce(rb.transform.forward * force, ForceMode.Impulse);
    }
    
    
    private IEnumerator DropBlockAtApex(Rigidbody rb) {
        // Wait until the ant is at the apex of its jump.
        yield return new WaitUntil(() => rb.velocity.y > 0);
        yield return new WaitUntil(() => Mathf.Abs(rb.velocity.y) < 0.1f);
        
        bool isDescending = rb.velocity.y <= 0;
        RaycastHit hit;
        bool isCloseToGround = Physics.Raycast(ant.transform.position, Vector3.down, out hit, 1.5f);

        if (isDescending && isCloseToGround && blockBelow is AirBlock) {
            Vector3Int blockPos = Vector3Int.RoundToInt(blockBelowPos);
            
            worldManager.SetBlock(blockPos.x, blockPos.y, blockPos.z, new MulchBlock());
            heldBlock = null;
        }
        
    }


    private void rotateAnt(Rigidbody rb) {
        rb.constraints &= ~RigidbodyConstraints.FreezeRotationY;
        ant.transform.Rotate(0, 45, 0);
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
            health = Mathf.Min(health + 10.0f,  100.0f);
            worldManager.SetBlock((int)blockBelowPos.x, (int)blockBelowPos.y, (int)blockBelowPos.z, new AirBlock());
        }
    }
    
    private void loseHealth() {
        if (isQueen) return;
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
        int y = Mathf.RoundToInt(ant.transform.position.y - 0.65f);
        int z = Mathf.RoundToInt(ant.transform.position.z);
        
        y = Mathf.Max(y, 0);

        blockBelow = worldManager.GetBlock(x, y, z);
        blockBelowPos = new Vector3(x, y, z);

        if (blockBelow != null) {
            DrawDebugRectangle(new Vector3(x, y + 0.5f, z)); 
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
    
    private void checkDeath() {
        // if is queen, don't die
        if (isQueen) {
            return;
        }
        
        if (health <= 0.0f || ant.transform.position.y < 0.0f) {
            Destroy(ant);
        }
    }
    
    public void setQueen (bool queen) {
        isQueen = queen;
    }
    
    public void createNest() {
        if (heldBlock is null && blockBelow is not ContainerBlock && blockBelow is not NestBlock) {
            health -= 33.0f;
            heldBlock = blockBelow;
            worldManager.SetBlock((int)blockBelowPos.x, (int)blockBelowPos.y, (int)blockBelowPos.z, new NestBlock());
            // insert block into nestPositions
            nestPositions.Add(blockBelowPos);
            
            //Debug.Log("Nested");
            
        }  else if (heldBlock is not null && blockBelow is AirBlock) {
            // set block below to held block
            worldManager.SetBlock((int)blockBelowPos.x, (int)blockBelowPos.y, (int)blockBelowPos.z, new MulchBlock());
            heldBlock = null;
        }
    }

    public Vector3 calculateAverageDirectionOfNest() {
        GameObject q = GameObject.FindGameObjectWithTag("Queen");
        List<Vector3> nestPos = q.GetComponent<antLogic>().nestPositions;
        
        Vector3 average = Vector3.zero;
        foreach (Vector3 pos in nestPos) {
            average += pos;
        }
        
        average /= nestPos.Count;
        return average;
    }


    
    private void pickUpBlock() {
        if (heldBlock is null && blockBelow is MulchBlock) {
            heldBlock = new MulchBlock();
            
            worldManager.SetBlock((int)blockBelowPos.x, (int)blockBelowPos.y, (int)blockBelowPos.z, new AirBlock());
        }
        
    }

    private void dropBlock() {
        if (blockBelow is not MulchBlock) {
            hopAndDrop();
        } 
        
    }

    private void UnstuckFall() {
        if (ant.transform.position.y < 0.0f) {
            // move ant to any random surface block position
            ant.transform.position = new Vector3(Random.Range(-15.0f, 15.0f), 12.0f, Random.Range(-15.0f, 15.0f));
        }
    }
    
    // breed ant: if worker ant touches queen ant, queen ant will give birth to a new worker ant
    private void OnCollisionEnter(Collision collision) {
        if (this.tag == "Queen" && collision.gameObject.tag == "Ant") {
            GameObject newAnt = Instantiate(ant, new Vector3(Random.Range(-15.0f, 15.0f), 12.0f, Random.Range(-15.0f, 15.0f)), Quaternion.identity);
            newAnt.tag = "Ant";
        }
    }

}