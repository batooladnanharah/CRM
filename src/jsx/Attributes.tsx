function Attributes() {
  return (
    <section className="lesson">
      <h2>3. JSX Attributes</h2>

      <img
        src="https://picsum.photos/250"
        alt="Random"
        width={250}
      />

      <p className="description">
        This image uses JSX attributes.
      </p>

      <input
        type="text"
        placeholder="Enter your name"
      />
    </section>
  );
}

export default Attributes;