import Header from "./components/Header";
import UserCard from "./components/UserCard";
import ProductCard from "./components/ProductCard";
import Footer from "./components/Footer";

function App() {
  return (
    <>
      <Header />

      <UserCard
        name="Batool"
        email="batool@example.com"
      />

      <UserCard
        name="John"
        email="john@example.com"
      />

      <ProductCard
        name="iPhone 16"
        price={999}
      />

      <ProductCard
        name="MacBook Pro"
        price={2499}
      />

      <Footer />
    </>
  );
}

export default App;