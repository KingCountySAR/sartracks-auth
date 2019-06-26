import React, { Component } from 'react';
import { Collapse, Container, Navbar, NavbarBrand, NavbarToggler, NavItem, NavLink, Button } from 'reactstrap';
import { Link } from 'react-router-dom';
import { connect } from 'react-redux';
import './NavMenu.css';
import userManager from '../user-manager';

class NavMenu extends Component {
  static displayName = NavMenu.name;

  constructor (props) {
    super(props);

    this.toggleNavbar = this.toggleNavbar.bind(this);
    this.state = {
      collapsed: true
    };
  }

  toggleNavbar () {
    this.setState({
      collapsed: !this.state.collapsed
    });
  }

  onLoginButtonClick(event) {
    event.preventDefault();
    userManager.signinRedirect();
  }

  onLogoutButtonClick(event) {
    event.preventDefault();
    userManager.signoutRedirect();
  }

  render () {
    const { oidc } = this.props;

    return (
      <header>
        <Navbar className="navbar-expand-sm navbar-toggleable-sm navbar-dark bg-dark border-bottom box-shadow mb-3" light>
          <Container>
            <NavbarBrand tag={'a'} href="/">SAR Tracks</NavbarBrand>
            <NavbarToggler onClick={this.toggleNavbar} className="mr-2" />
            <Collapse className="d-sm-inline-flex flex-sm-row-reverse" isOpen={!this.state.collapsed} navbar>
              <ul className="navbar-nav flex-grow">
                <NavItem>
                  <NavLink tag={Link} to="/">Home</NavLink>
                </NavItem>
                <NavItem>
                  <NavLink tag={Link} to="/counter">Counter</NavLink>
                </NavItem>
                <NavItem className="flex-grow"></NavItem>
                {oidc.user && oidc.user.profile ? <NavLink tag={Link} to="/manage">Hello {oidc.user.profile.name}</NavLink> : null}
                <NavItem>
                  {oidc.isLoadingUser
                  ? <span className='navbar-text'>Loading <i className='fas fa-spin fa-spinner'></i></span>
                  : oidc.user 
                  ? <NavLink tag={Button} className="btn btn-link" onClick={this.onLogoutButtonClick}>Log out</NavLink>
                  : <NavLink tag={Button} className="btn btn-link" onClick={this.onLoginButtonClick}>Log in</NavLink>
                  }
                </NavItem>
              </ul>
            </Collapse>
          </Container>
        </Navbar>
      </header>
    );
  }
}

function mapStateToProps(state) {
  return {
    oidc: state.oidc
  };
}

export default connect(mapStateToProps)(NavMenu);