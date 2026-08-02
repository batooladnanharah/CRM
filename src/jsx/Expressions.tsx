function Expressions() {
  const name = "Batool";
  const age = 26;
  const country = "Jordan";

  return (
    <section className="lesson">
      <h2>2. JavaScript Expressions</h2>

      <p>Name: {name}</p>

      <p>Age: {age}</p>

      <p>Country: {country}</p>

      <p>Next Year Age: {age + 1}</p>

      <p>{name.toUpperCase()}</p>

      <p>Current Time: {new Date().toLocaleTimeString()}</p>
    </section>
  );
}

export default Expressions;