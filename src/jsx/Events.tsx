function Events() {
  function handleClick() {
    alert("Button Clicked!");
  }

  function handleMouseEnter() {
    console.log("Mouse entered");
  }

  return (
    <section className="lesson">
      <h2>7. Events</h2>

      <button onClick={handleClick}>
        Click Me
      </button>

      <br />

      <button onMouseEnter={handleMouseEnter}>
        Hover Me
      </button>
    </section>
  );
}

export default Events;