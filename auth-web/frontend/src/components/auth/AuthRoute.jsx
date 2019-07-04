import React, {Component} from "react";
import { Route } from "react-router-dom";
//import PropTypes from "prop-types";
import { connect } from "react-redux";
import userManager from '../../user-manager';

class ForceLogin extends Component {
  constructor(props) {
    super(props)
    this.redirect = props.redirect;
  }

  componentDidMount() {
    sessionStorage.setItem('redirect', this.redirect);
    userManager.signinRedirect();
  }

  render() {
    return 'Redirecting to login page ...';
  }
}

const AuthRoute = React.memo(({ component: Component, denied, loading, oidc, ...rest }) => {
  return (
    <Route
      {...rest}
      render={props => {
        if ((oidc.isLoadingUser && !oidc.preload) || oidc.isSigningOut) {
          return loading || <i className='fas fa-spinner fa-spin'></i>;
        } else if (!oidc.user) {
          return denied || (denied === '' ? denied : <ForceLogin redirect={props.match.url} />);
        } else {
          return <Component {...rest} />;
        }
      }}
    />
  );
});

// AuthRoute.propTypes = {
//   denied: PropTypes.node,
//   component: PropTypes.func.isRequired,
//   oidc: PropTypes.object.isRequired
// };

function mapStateToProps(state) {
  return {
    oidc: state.oidc
  };
}

export default connect(mapStateToProps)(AuthRoute);