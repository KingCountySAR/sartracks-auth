import { reducer as oidc } from 'redux-oidc';

export const PRELOAD = 'oidc/PRELOAD';
export const SIGNING_OUT = 'oidc/SIGNING_OUT';
//export const RECEIVE_ROLES = 'RECEIVE_ROLES';

export const ActionsFactory = (userManager) => ({
  doSignout: () => (dispatch) => {
    dispatch({type: SIGNING_OUT })
    userManager.signoutRedirect();
  },

  preloadUser: payload => ({ type: PRELOAD, payload })
})

export function reducer(state = {}, action) {
  const newState = oidc(state, action)

  switch (action.type) {
    case PRELOAD:
      return {...newState, ...action.payload, preload: true, signedIn: true, isLoadingUser: true }

    case SIGNING_OUT:
      return ({...newState, isSigningOut: true});

    case 'redux-oidc/SESSION_TERMINATED':
      // Don't remove the isSigningOut flag, or AuthRoute may force sign in again on next render.
      return newState;

    default:
      return newState.isSigningOut ? {...newState, isSigningOut: false } : newState;
  }
}