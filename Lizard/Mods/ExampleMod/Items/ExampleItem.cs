// mod items MUST inherit from either the Item class or one of its subclasses.
// mod items MUST also implement the IModItem interface
public class ExampleItem : Item, IModItem
{
	// these strings MUST be defined by mod items
	// the example link in description is for items that reference other items, such as 'Graduation Cap'
	public string DisplayName { get { return "Example Item"; } }
	public string Description { get { return "Example item description.\n[<color=#FFCC00>Example Link</color>]"; } }
	public string ItemImage { get { return "ExampleItem.png"; } }
	
	protected int regenPerTick = 2;		// the amount to heal per iteration
	protected float tickDelay = 0.5f;	// the delay between each heal
	
	protected bool activated = false;	// if the heal loop has been started
	
	// none of the functions in this class need to be marked virtual
	// its only if you want to override those functions in a derived class
	// overridden functions can also be overridden by derived classes
	
	// constructor, runs when the item is created in code
	public ExampleItem()
	{
		// setup basic item data here
		// Do not set Item.ID as Lizard will assign it right after the end of this function
		category = Item.Category.Defense;
	}
	
	// this function MUST be defined by mod objects
	// this function runs after Lizard assigns the items ID and adds it to the games data
	public virtual void Init()
	{
		// setup other things in here
		// things that require Item.ID, such as NumVarStatMod instances should be set in here
	}
	
	// Wizard of Legend function, called when the items effect should first be applied to the player
	// MUST be public override
	public override void Activate()
	{
		// return if not player or heal loop is already active
		if (!SetParentAsPlayer() || activated)
			return;
		
		// start heal loop and mark as active
		OnTick();
		activated = true;
	}
	
	// Wizard of Legend function, called when the items effect should be removed from the player
	// MUST be public override
	public override void Deactivate()
	{
		// return if not player
		if (!SetParentAsPlayer())
			return;
		
		// mark as not active
		activated = false;
	}
	
	// convenience function that just calls the next heal loop iteration
	protected virtual void OnTick()
	{
		// call delayed heal
		parentPlayer.StartCoroutine(TickAI());
		
		// StartCoroutine is a Unity3D function which calls a
		// function at the end of a frame, with an optional delay
	}
	
	// StartCoroutine requires the target function to return an IEnumerator,
	// this function signature could be changed to
	// protected virtual IEnumerator TickAI()
	// if a using statement was added to ExampleMod/Usings.cs
	protected virtual System.Collections.IEnumerator TickAI()
	{
		// wait for next heal tick, this won't block other code from executing
        yield return new WaitForSeconds(tickDelay);
		
		// the code below only runs once the wait has finished
		
		// heal player
		parentPlayer.health.healthStat.CurrentValue += regenPerTick;
		
		// setup next tick if item still equipped
		if (activated)
			OnTick();
		
		// exit this iteration of the loop
		yield break;
	}
}