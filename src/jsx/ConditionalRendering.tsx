function ConditionalRendering() {
  const isLoggedIn = true;

  return (
    <section className="lesson">
      <h2>4. Conditional Rendering</h2>

      {isLoggedIn ? (
        <h3>Welcome Back!</h3>
      ) : (
        <h3>Please Login</h3>
      )}

      {isLoggedIn && <p>You have successfully logged in.</p>}
    </section>
  );
}

export default ConditionalRendering;